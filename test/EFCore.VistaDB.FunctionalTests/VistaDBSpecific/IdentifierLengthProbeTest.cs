// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.EntityFrameworkCore.TestUtilities;
using VistaDB.Provider;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Bisects VistaDB's maximum identifier length by trying progressively longer table/index names
///     and recording where the engine rejects with <c>Error 152: Invalid name or alias</c>.
///     EF Core's default <see cref="RelationalMaxIdentifierLengthConvention"/> limits names to 128
///     characters (SQL Server default). VistaDB's limit is shorter — this probe finds the exact value
///     so we can register the right convention in <c>VistaDBConventionSetBuilder</c>.
/// </summary>
public class IdentifierLengthProbeTest
{
    private readonly ITestOutputHelper _out;

    public IdentifierLengthProbeTest(ITestOutputHelper @out)
        => _out = @out;

    [VistaDBInstalledFact]
    public void Find_max_identifier_length()
    {
        using var file = new TempVistaDBFile();
        using (var ctx = new ProbeContext(file.ConnectionString))
        {
            ctx.Database.EnsureCreated();
        }

        using var conn = new VistaDBConnection(file.ConnectionString);
        conn.Open();

        Exec(conn, "CREATE TABLE [Base] ([Id] int NOT NULL IDENTITY(1, 1), [V] int NULL, CONSTRAINT [PK_Base] PRIMARY KEY ([Id]));");

        // Identifier length probe: try table names of varying lengths.
        foreach (var len in new[] { 128, 129, 130, 140, 150, 160, 192, 200, 256 })
        {
            var name = new string('T', len);
            try
            {
                Exec(conn, $"CREATE TABLE [{name}] ([Id] int NOT NULL);");
                _out.WriteLine($"  TABLE name length {len}: OK");
                Exec(conn, $"DROP TABLE [{name}];");
            }
            catch (Exception ex)
            {
                _out.WriteLine($"  TABLE name length {len}: REJECTED — {ex.Message.Split('\n')[0]}");
            }
        }

        _out.WriteLine("");
        _out.WriteLine("--- Reserved/special character probe ---");
        // The actual failing GraphUpdates identifier ends with ~ (EF Core's truncation suffix).
        // Test whether VistaDB rejects ~ in identifiers.
        foreach (var name in new[] { "IX_Test_a~", "IX_Test_x~hash", "IX_Test_x_hash", "IX_Test_ASCII" })
        {
            try
            {
                Exec(conn, $"CREATE INDEX [{name}] ON [Base] ([V]);");
                _out.WriteLine($"  Name '{name}': OK");
                Exec(conn, $"DROP INDEX [{name}] ON [Base];");
            }
            catch (Exception ex)
            {
                _out.WriteLine($"  Name '{name}': REJECTED — {ex.Message.Split('\n')[0]}");
            }
        }

        _out.WriteLine("");
        _out.WriteLine("--- Real failing identifier from GraphUpdates ---");
        // Exactly 128 characters, ends with ~ (EF Core's truncation marker).
        var failingName = "IX_UnidirectionalEntityCompositeKeyUnidirectionalEntityRoot_UnidirectionalEntityCompositeKeyKey1_UnidirectionalEntityCompositeK~";
        _out.WriteLine($"  Length: {failingName.Length}");
        try
        {
            Exec(conn, $"CREATE INDEX [{failingName}] ON [Base] ([V]);");
            _out.WriteLine($"  CREATE INDEX with real failing name: OK");
        }
        catch (Exception ex)
        {
            _out.WriteLine($"  CREATE INDEX with real failing name: REJECTED — {ex.Message.Split('\n')[0]}");
        }

        _out.WriteLine("");
        _out.WriteLine("--- Index name length probe ---");
        foreach (var len in new[] { 128, 129, 130, 140, 150, 160, 192, 200, 256 })
        {
            var name = new string('I', len);
            try
            {
                Exec(conn, $"CREATE INDEX [{name}] ON [Base] ([V]);");
                _out.WriteLine($"  INDEX name length {len}: OK");
                Exec(conn, $"DROP INDEX [{name}] ON [Base];");
            }
            catch (Exception ex)
            {
                _out.WriteLine($"  INDEX name length {len}: REJECTED — {ex.Message.Split('\n')[0]}");
            }
        }

        _out.WriteLine("");
        _out.WriteLine("--- Column name length probe ---");
        foreach (var len in new[] { 128, 129, 130, 140, 150, 160, 192, 200, 256 })
        {
            var name = new string('C', len);
            try
            {
                Exec(conn, $"CREATE TABLE [TestCol_{len}] ([Id] int NOT NULL, [{name}] int NULL);");
                _out.WriteLine($"  COLUMN name length {len}: OK");
                Exec(conn, $"DROP TABLE [TestCol_{len}];");
            }
            catch (Exception ex)
            {
                _out.WriteLine($"  COLUMN name length {len}: REJECTED — {ex.Message.Split('\n')[0]}");
            }
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
