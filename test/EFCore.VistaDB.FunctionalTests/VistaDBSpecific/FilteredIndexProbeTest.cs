// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.EntityFrameworkCore.TestUtilities;
using VistaDB.Provider;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Probes whether VistaDB accepts a filtered unique index syntax
///     (<c>CREATE UNIQUE INDEX … WHERE [col] IS NOT NULL</c>). This is how SQL Server makes
///     unique indexes coexist with multi-NULL rows on nullable columns — without it, both
///     engines treat NULL as a value and reject the second NULL as a duplicate-key violation
///     (VistaDB error 309). If VistaDB rejects the filter syntax, we'll need to fall back to
///     emitting non-unique indexes for unique-on-nullable-column scenarios.
/// </summary>
public class FilteredIndexProbeTest
{
    private readonly ITestOutputHelper _out;

    public FilteredIndexProbeTest(ITestOutputHelper @out)
        => _out = @out;

    [VistaDBInstalledFact]
    public void Filtered_unique_index_with_where_is_not_null()
    {
        using var file = new TempVistaDBFile();

        using (var ctx = new ProbeContext(file.ConnectionString))
        {
            ctx.Database.EnsureCreated();
        }

        using var conn = new VistaDBConnection(file.ConnectionString);
        conn.Open();

        Exec(conn, @"
            CREATE TABLE [Probe] (
                [Id] int NOT NULL IDENTITY(1, 1),
                [FkA] int NULL,
                CONSTRAINT [PK_Probe] PRIMARY KEY ([Id])
            );");

        try
        {
            Exec(conn, "CREATE UNIQUE INDEX [IX_Probe_FkA_filtered] ON [Probe] ([FkA]) WHERE [FkA] IS NOT NULL;");
            _out.WriteLine("Filtered unique index CREATED successfully.");

            // Verify behavior: insert two rows with NULL — should NOT be rejected if the filter excludes them.
            Exec(conn, "INSERT INTO [Probe] ([FkA]) VALUES (NULL);");
            Exec(conn, "INSERT INTO [Probe] ([FkA]) VALUES (NULL);");
            _out.WriteLine("Two NULL rows accepted under filtered unique index.");

            // Insert two rows with the same non-NULL value — should be REJECTED (uniqueness on non-NULL holds).
            Exec(conn, "INSERT INTO [Probe] ([FkA]) VALUES (5);");
            try
            {
                Exec(conn, "INSERT INTO [Probe] ([FkA]) VALUES (5);");
                _out.WriteLine("*** Second duplicate non-NULL insert was NOT rejected. Filter NOT working.");
            }
            catch (Exception ex)
            {
                _out.WriteLine($"Second duplicate non-NULL insert rejected (expected): {ex.GetType().Name}: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            _out.WriteLine($"*** Filtered index syntax REJECTED: {ex.GetType().Name}: {ex.Message}");
            _out.WriteLine($"*** Inner: {ex.InnerException?.Message}");
        }

        _out.WriteLine("");
        _out.WriteLine("--- Alternative: plain non-unique index ---");

        // The filtered index was rejected above (expected for VistaDB); DROP would error so
        // wrap it best-effort.
        try { Exec(conn, "DROP INDEX [IX_Probe_FkA_filtered] ON [Probe];"); } catch { }
        Exec(conn, "CREATE INDEX [IX_Probe_FkA] ON [Probe] ([FkA]);");
        _out.WriteLine("Non-unique index created OK.");
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
