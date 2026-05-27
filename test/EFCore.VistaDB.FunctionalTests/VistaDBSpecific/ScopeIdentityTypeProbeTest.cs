// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.EntityFrameworkCore.TestUtilities;
using VistaDB.Provider;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Probes the runtime type of <c>scope_identity()</c> in VistaDB and whether comparing
///     it directly to an INT column triggers engine error 558 ("Invalid operand(s) data type
///     for operator: =") — and whether <c>CAST(scope_identity() AS INT)</c> resolves it.
///     The EF Core base WHERE-clause-based read-back (used when read-backs include more than
///     just the IDENTITY column) emits <c>WHERE [Id] = scope_identity()</c>; if VistaDB
///     considers these operand types incompatible, we need to wrap in CAST.
/// </summary>
public class ScopeIdentityTypeProbeTest
{
    private readonly ITestOutputHelper _out;

    public ScopeIdentityTypeProbeTest(ITestOutputHelper @out)
        => _out = @out;

    [VistaDBInstalledFact]
    public void Probe_scope_identity_type_and_cast_workaround()
    {
        using var file = new TempVistaDBFile();
        using (var ctx = new ProbeContext(file.ConnectionString))
        {
            ctx.Database.EnsureCreated();
        }

        using var conn = new VistaDBConnection(file.ConnectionString);
        conn.Open();

        Exec(conn, "CREATE TABLE [Probe] ([Id] int NOT NULL IDENTITY(1, 1), [V] int NULL, CONSTRAINT [PK_Probe] PRIMARY KEY ([Id]));");
        Exec(conn, "CREATE TABLE [ProbeBig] ([Id] bigint NOT NULL IDENTITY(1, 1), [V] int NULL, CONSTRAINT [PK_ProbeBig] PRIMARY KEY ([Id]));");
        Exec(conn, "CREATE TABLE [ProbeStr] ([Id] nvarchar(50) NOT NULL, [V] int NULL, CONSTRAINT [PK_ProbeStr] PRIMARY KEY ([Id]));");

        // Variant 1: typeof(scope_identity())
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO [Probe] ([V]) VALUES (10); SELECT scope_identity() AS si;";
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                _out.WriteLine($"  scope_identity() reported as type {r.GetDataTypeName(0)} ({r.GetFieldType(0)}), value = {r[0]}");
            }
        }

        // Variant 2: WHERE [Id] = scope_identity() — direct comparison
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO [Probe] ([V]) VALUES (20); SELECT [Id] FROM [Probe] WHERE [Id] = scope_identity();";
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                _out.WriteLine($"  WHERE [Id] = scope_identity() OK, returned Id = {r[0]}");
            }
            else
            {
                _out.WriteLine("  WHERE [Id] = scope_identity() returned 0 rows");
            }
        }
        catch (Exception ex)
        {
            _out.WriteLine($"  WHERE [Id] = scope_identity() FAILED: {ex.Message.Split('\n')[0]}");
        }

        // Variant 3: WHERE [Id] = CAST(scope_identity() AS INT)
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO [Probe] ([V]) VALUES (30); SELECT [Id] FROM [Probe] WHERE [Id] = CAST(scope_identity() AS INT);";
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                _out.WriteLine($"  WHERE [Id] = CAST(scope_identity() AS INT) OK, returned Id = {r[0]}");
            }
            else
            {
                _out.WriteLine("  CAST variant returned 0 rows");
            }
        }
        catch (Exception ex)
        {
            _out.WriteLine($"  CAST variant FAILED: {ex.Message.Split('\n')[0]}");
        }

        // Variant 4: comparison via CONVERT
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO [Probe] ([V]) VALUES (40); SELECT [Id] FROM [Probe] WHERE [Id] = CONVERT(int, scope_identity());";
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                _out.WriteLine($"  WHERE [Id] = CONVERT(int, scope_identity()) OK, returned Id = {r[0]}");
            }
            else
            {
                _out.WriteLine("  CONVERT variant returned 0 rows");
            }
        }
        catch (Exception ex)
        {
            _out.WriteLine($"  CONVERT variant FAILED: {ex.Message.Split('\n')[0]}");
        }

        // Variant: BIGINT Id column
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO [ProbeBig] ([V]) VALUES (100); SELECT [Id] FROM [ProbeBig] WHERE [Id] = scope_identity();";
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                _out.WriteLine($"  BIGINT: WHERE [Id] = scope_identity() OK, returned Id = {r[0]}");
            }
            else
            {
                _out.WriteLine("  BIGINT: returned 0 rows");
            }
        }
        catch (Exception ex)
        {
            _out.WriteLine($"  BIGINT FAILED: {ex.Message.Split('\n')[0]}");
        }

        // Variant: STRING PK with explicit-value INSERT
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO [ProbeStr] ([Id], [V]) VALUES ('alpha', 100); SELECT [Id] FROM [ProbeStr] WHERE [Id] = 'alpha';";
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                _out.WriteLine($"  STRING PK explicit: WHERE [Id] = 'alpha' OK, returned Id = {r[0]}");
            }
        }
        catch (Exception ex)
        {
            _out.WriteLine($"  STRING PK FAILED: {ex.Message.Split('\n')[0]}");
        }

        // Variant: STRING PK with parameter
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO [ProbeStr] ([Id], [V]) VALUES (@p0, 200); SELECT [Id] FROM [ProbeStr] WHERE [Id] = @p0;";
            var p0 = cmd.CreateParameter(); p0.ParameterName = "@p0"; p0.Value = "beta"; cmd.Parameters.Add(p0);
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                _out.WriteLine($"  STRING PK param: OK, returned Id = {r[0]}");
            }
        }
        catch (Exception ex)
        {
            _out.WriteLine($"  STRING PK param FAILED: {ex.Message.Split('\n')[0]}");
        }

        // Variant 5: WHERE 1 = 1 AND [Id] = scope_identity() — the actual EF Core shape (line 5 of failing SQL)
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO [Probe] ([V]) VALUES (50);\r\nSELECT [Id]\r\nFROM [Probe]\r\nWHERE @@ROWCOUNT = 1 AND [Id] = scope_identity();";
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                _out.WriteLine($"  EF-shape (@@ROWCOUNT = 1 AND [Id] = scope_identity()) OK, returned Id = {r[0]}");
            }
            else
            {
                _out.WriteLine("  EF-shape returned 0 rows");
            }
        }
        catch (Exception ex)
        {
            _out.WriteLine($"  EF-shape FAILED: {ex.Message.Split('\n')[0]}");
        }
    }

    private static void Exec(VistaDBConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private class ProbeContext : DbContext
    {
        private readonly string _cs;
        public ProbeContext(string cs) => _cs = cs;
        protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseVistaDB(_cs);
    }
}
