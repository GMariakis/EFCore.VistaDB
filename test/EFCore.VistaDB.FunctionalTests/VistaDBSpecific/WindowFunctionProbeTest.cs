// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.EntityFrameworkCore.TestUtilities;
using VistaDB.Provider;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Probes whether VistaDB supports the SQL window function <c>ROW_NUMBER() OVER(PARTITION BY ...)</c>
///     used by EF Core's split-/paginated- collection queries. ManyToMany "partially loaded" tests
///     emit this pattern; if VistaDB rejects it, those tests are inherently unsupported.
/// </summary>
public class WindowFunctionProbeTest
{
    private readonly ITestOutputHelper _out;

    public WindowFunctionProbeTest(ITestOutputHelper @out)
        => _out = @out;

    [VistaDBInstalledFact]
    public void Probe_row_number_over()
    {
        using var file = new TempVistaDBFile();
        using (var ctx = new ProbeContext(file.ConnectionString))
        {
            ctx.Database.EnsureCreated();
        }

        using var conn = new VistaDBConnection(file.ConnectionString);
        conn.Open();

        Exec(conn, "CREATE TABLE [Probe] ([G] int NOT NULL, [V] int NOT NULL);");
        Exec(conn, "INSERT INTO [Probe] ([G], [V]) VALUES (1, 10), (1, 20), (2, 30), (2, 40), (2, 50);");

        // Variant: bare ROW_NUMBER()
        Try(conn, "ROW_NUMBER", "SELECT [G], [V], ROW_NUMBER() OVER (ORDER BY [V]) AS [rn] FROM [Probe];");

        // Variant: PARTITION BY ROW_NUMBER
        Try(conn, "PARTITION BY", "SELECT [G], [V], ROW_NUMBER() OVER (PARTITION BY [G] ORDER BY [V]) AS [rn] FROM [Probe];");

        // Variant: alternative — OFFSET FETCH (LIMIT-style pagination)
        Try(conn, "OFFSET FETCH", "SELECT [G], [V] FROM [Probe] ORDER BY [V] OFFSET 1 ROWS FETCH NEXT 2 ROWS ONLY;");

        // Variant: TOP
        Try(conn, "TOP(n)", "SELECT TOP(2) [G], [V] FROM [Probe] ORDER BY [V];");
    }

    private static void Exec(VistaDBConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private void Try(VistaDBConnection conn, string label, string sql)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            using var r = cmd.ExecuteReader();
            var count = 0;
            while (r.Read())
            {
                count++;
            }
            _out.WriteLine($"  {label}: OK ({count} rows)");
        }
        catch (Exception ex)
        {
            _out.WriteLine($"  {label}: FAILED — {ex.Message.Split('\n')[0]}");
        }
    }

    private class ProbeContext : DbContext
    {
        private readonly string _cs;
        public ProbeContext(string cs) => _cs = cs;
        protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseVistaDB(_cs);
    }
}
