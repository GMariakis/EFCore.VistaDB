// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestUtilities;
using VistaDB.DDA;
using VistaDB.Provider;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Determines whether setting <see cref="IVistaDBTable.IdentityInsert" /> via the DDA API also
///     enables explicit-value INSERTs issued through the parallel ADO.NET / SQL path. The xmldoc
///     calls the DDA setting "independent from the one-table-at-a-time setting for a SQL connection" —
///     this probe verifies whether "independent" means "doesn't conflict with" (both work) or
///     "doesn't affect" (DDA setting applies only to DDA-path inserts).
/// </summary>
public class DdaIdentityInsertProbeTest
{
    private readonly ITestOutputHelper _out;

    public DdaIdentityInsertProbeTest(ITestOutputHelper @out)
        => _out = @out;

    [VistaDBInstalledFact]
    public void Dda_IdentityInsert_toggle_affects_subsequent_SQL_insert()
    {
        using var file = new TempVistaDBFile();

        // 1) Create the database file via EnsureCreated.
        using (var ctx = new ProbeContext(file.ConnectionString))
        {
            ctx.Database.EnsureCreated();
        }

        // 2) Open the table via DDA and turn IdentityInsert ON.
        var ddaRoot = global::VistaDB.DDA.VistaDBEngine.Connections.OpenDDA();
        using (var database = ddaRoot.OpenDatabase(file.FilePath, global::VistaDB.VistaDBDatabaseOpenMode.MultiProcessReadWrite, null))
        using (var table = database.OpenTable("Probes", exclusive: false, readOnly: false))
        {
            table.IdentityInsert = true;
            _out.WriteLine($"Set Probes.IdentityInsert = true via DDA");
            // Do NOT set to false yet; leave it on across the SQL insert.
        }

        // 3) Now try an explicit-Id SQL INSERT WITHOUT any SET IDENTITY_INSERT statement.
        using var conn = new VistaDBConnection(file.ConnectionString);
        conn.Open();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO [Probes] ([Id], [Name]) VALUES (999, 'dda-toggled');";
            try
            {
                var affected = cmd.ExecuteNonQuery();
                _out.WriteLine($"SQL INSERT (no SET IDENTITY_INSERT) — ExecuteNonQuery returned {affected}");
            }
            catch (Exception ex)
            {
                _out.WriteLine($"SQL INSERT failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        // 4) Verify what's in the table.
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT [Id], [Name] FROM [Probes]";
            using var reader = cmd.ExecuteReader();
            var rowCount = 0;
            while (reader.Read())
            {
                rowCount++;
                _out.WriteLine($"  Row {rowCount}: Id={reader.GetInt32(0)} Name={reader.GetString(1)}");
            }

            _out.WriteLine($"Total rows: {rowCount}");
        }
    }

    private class ProbeContext : DbContext
    {
        private readonly string _cs;
        public ProbeContext(string cs) => _cs = cs;
        public DbSet<Probe> Probes => Set<Probe>();
        protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseVistaDB(_cs);
    }

    private class Probe
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
