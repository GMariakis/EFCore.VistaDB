// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.EntityFrameworkCore.TestUtilities;
using VistaDB.Provider;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Direct VistaDB-SQL probe: does <c>INSERT … ; SELECT [Id] FROM [T] WHERE @@ROWCOUNT = 1 AND
///     [Id] = scope_identity();</c> (the exact shape EF Core's <c>UpdateAndSelectSqlGenerator</c>
///     emits) actually return the inserted row in VistaDB? Reproduces the GraphUpdates seed-failure
///     scenario where the final logged INSERT was for an OptionalSingle1Derived row whose
///     accompanying SELECT-back apparently returned 0 rows, tripping DbUpdateConcurrencyException.
/// </summary>
public class ScopeIdentityProbeTest
{
    private readonly ITestOutputHelper _out;

    public ScopeIdentityProbeTest(ITestOutputHelper @out)
        => _out = @out;

    [VistaDBInstalledFact]
    public void InsertWithSelectBack_returns_inserted_row()
    {
        using var file = new TempVistaDBFile();

        // TempVistaDBFile only provides a path; create the actual .vdb6 by using EF Core's
        // DatabaseCreator path (the same one we use everywhere else).
        using (var ctx = new ProbeContext(file.ConnectionString))
        {
            ctx.Database.EnsureCreated();
        }

        using var conn = new VistaDBConnection(file.ConnectionString);
        conn.Open();

        Exec(conn, @"
            CREATE TABLE [OptSingle] (
                [Id] int NOT NULL IDENTITY(1, 1),
                [RootId] int NULL,
                [Discriminator] nvarchar(34) NOT NULL,
                [DerivedRootId] int NULL,
                CONSTRAINT [PK_OptSingle] PRIMARY KEY ([Id])
            );");

        // First INSERT — base type with RootId = 1, no DerivedRootId
        var sql1 = @"INSERT INTO [OptSingle] ([Discriminator], [RootId]) VALUES (@p0, @p1); SELECT [Id] FROM [OptSingle] WHERE @@ROWCOUNT = 1 AND [Id] = scope_identity();";
        var id1 = ExecQueryFirst(conn, sql1, ("@p0", "OptionalSingle1"), ("@p1", 1));
        _out.WriteLine($"INSERT #1 (base type, RootId=1) returned Id = {(id1 ?? "<null>")}");

        // Second INSERT — derived type with DerivedRootId = 1, RootId = NULL
        var sql2 = @"INSERT INTO [OptSingle] ([DerivedRootId], [Discriminator], [RootId]) VALUES (@p0, @p1, @p2); SELECT [Id] FROM [OptSingle] WHERE @@ROWCOUNT = 1 AND [Id] = scope_identity();";
        var id2 = ExecQueryFirst(conn, sql2, ("@p0", 1), ("@p1", "OptionalSingle1Derived"), ("@p2", (object?)DBNull.Value));
        _out.WriteLine($"INSERT #2 (derived type, RootId=NULL, DerivedRootId=1) returned Id = {(id2 ?? "<null>")}");

        // Diagnostic: read all rows back
        _out.WriteLine("--- Rows in [OptSingle] after both inserts ---");
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT [Id], [Discriminator], [RootId], [DerivedRootId] FROM [OptSingle]";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                _out.WriteLine($"  Id={reader[0]} Discriminator={reader[1]} RootId={(reader.IsDBNull(2) ? "NULL" : reader[2].ToString())} DerivedRootId={(reader.IsDBNull(3) ? "NULL" : reader[3].ToString())}");
            }
        }

        // Also probe what @@ROWCOUNT and scope_identity() report after the insert.
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO [OptSingle] ([DerivedRootId], [Discriminator], [RootId]) VALUES (@p0, @p1, NULL); SELECT @@ROWCOUNT AS R, scope_identity() AS S;";
            var p0 = cmd.CreateParameter(); p0.ParameterName = "@p0"; p0.Value = 2; cmd.Parameters.Add(p0);
            var p1 = cmd.CreateParameter(); p1.ParameterName = "@p1"; p1.Value = "OptionalSingle1Derived"; cmd.Parameters.Add(p1);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                _out.WriteLine($"@@ROWCOUNT = {reader["R"]}, scope_identity() = {reader["S"]}");
            }
        }

        Assert.NotNull(id1);
        Assert.NotNull(id2);
    }

    private class ProbeContext : DbContext
    {
        private readonly string _cs;
        public ProbeContext(string cs) => _cs = cs;
        protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseVistaDB(_cs);
        // empty model; EnsureCreated still creates the .vdb6 file
    }

    private static void Exec(VistaDBConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static string? ExecQueryFirst(VistaDBConnection conn, string sql, params (string Name, object? Value)[] parameters)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (n, v) in parameters)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = n;
            p.Value = v ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return reader[0]?.ToString();
        }
        return null;
    }
}
