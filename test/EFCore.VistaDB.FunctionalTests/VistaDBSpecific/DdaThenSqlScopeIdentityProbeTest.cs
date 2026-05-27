// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.EntityFrameworkCore.TestUtilities;
using VistaDB.Provider;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Probes whether a DDA-routed Insert/Put/Post against an IDENTITY table corrupts the SQL
///     connection's <c>scope_identity()</c> state. The GraphUpdates seed mixes DDA-routed explicit-Id
///     INSERTs (Bloog Id=515, Poost Id=516, 517) with SQL auto-Id INSERTs that rely on
///     <c>SELECT [Id] FROM [T] WHERE @@ROWCOUNT = 1 AND [Id] = scope_identity()</c>. If DDA's
///     Post-commit clears the session-level scope, the subsequent SQL INSERTs see
///     <c>scope_identity()</c> return NULL and the SELECT-back returns 0 rows, manifesting as the
///     DbUpdateConcurrencyException observed in fixture init.
/// </summary>
public class DdaThenSqlScopeIdentityProbeTest
{
    private readonly ITestOutputHelper _out;

    public DdaThenSqlScopeIdentityProbeTest(ITestOutputHelper @out)
        => _out = @out;

    [VistaDBInstalledFact]
    public void Dda_insert_then_sql_insert_scope_identity_returns_correct_value()
    {
        using var file = new TempVistaDBFile();

        using (var ctx = new ProbeContext(file.ConnectionString))
        {
            ctx.Database.EnsureCreated();
        }

        // Step 1: DDA insert with explicit Id = 515 (analog of Bloog Id=515 in real seed)
        using (var dda = global::VistaDB.DDA.VistaDBEngine.Connections.OpenDDA())
        using (var db = dda.OpenDatabase(file.FilePath, global::VistaDB.VistaDBDatabaseOpenMode.SingleProcessReadWrite, null))
        {
            using var table = db.OpenTable("ProbeTable", exclusive: false, readOnly: false);
            table.IdentityInsert = true;
            try
            {
                table.Insert();
                table.PutInt32("Id", 515);
                table.PutString("Name", "DDA-inserted");
                table.Post();
            }
            finally
            {
                table.IdentityInsert = false;
            }
        }

        // Step 2: now open a new SQL connection and INSERT (auto-Id) + SELECT scope_identity()
        using var conn = new VistaDBConnection(file.ConnectionString);
        conn.Open();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO [ProbeTable] ([Name]) VALUES (@p0); SELECT @@ROWCOUNT AS R, scope_identity() AS S;";
            var p0 = cmd.CreateParameter(); p0.ParameterName = "@p0"; p0.Value = "SQL-inserted-A"; cmd.Parameters.Add(p0);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                _out.WriteLine($"After SQL INSERT #A: @@ROWCOUNT = {reader["R"]}, scope_identity() = {reader["S"]}");
            }
        }

        // Step 3: the EF-Core-style INSERT + SELECT [Id] FROM [T] WHERE @@ROWCOUNT = 1 AND [Id] = scope_identity()
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO [ProbeTable] ([Name]) VALUES (@p0); SELECT [Id] FROM [ProbeTable] WHERE @@ROWCOUNT = 1 AND [Id] = scope_identity();";
            var p0 = cmd.CreateParameter(); p0.ParameterName = "@p0"; p0.Value = "SQL-inserted-B"; cmd.Parameters.Add(p0);
            using var reader = cmd.ExecuteReader();
            var rowCount = 0;
            while (reader.Read())
            {
                _out.WriteLine($"After SQL INSERT #B (EF-shape): Id = {reader[0]}");
                rowCount++;
            }
            _out.WriteLine($"SELECT-back returned {rowCount} rows. Expected: 1.");
            Assert.Equal(1, rowCount);
        }

        // Step 4: dump all rows
        _out.WriteLine("--- All rows ---");
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT [Id], [Name] FROM [ProbeTable]";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                _out.WriteLine($"  Id={reader[0]} Name={reader[1]}");
            }
        }
    }

    private class ProbeContext : DbContext
    {
        private readonly string _cs;
        public ProbeContext(string cs) => _cs = cs;
        protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseVistaDB(_cs);
        public DbSet<ProbeEntity> ProbeTable => Set<ProbeEntity>();
    }

    private class ProbeEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
