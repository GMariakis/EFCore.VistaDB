// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.EntityFrameworkCore.TestUtilities;
using VistaDB.Provider;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Probe what savepoint syntax VistaDB supports, if any. EF Core's relational pipeline emits
///     savepoints during nested SaveChanges scenarios; the GraphUpdates fixture failure surfaces
///     <c>Error 617: Name or alias cannot be reserved word: SAVE</c> when our SqlGenerationHelper
///     emits <c>SAVE TRANSACTION [savepoint_name]</c>. If VistaDB rejects both <c>SAVE TRANSACTION</c>
///     and <c>SAVEPOINT</c>, the right answer is to declare savepoints unsupported and stop EF Core
///     from emitting them.
/// </summary>
public class SavepointSupportProbeTest
{
    private readonly ITestOutputHelper _out;

    public SavepointSupportProbeTest(ITestOutputHelper @out)
        => _out = @out;

    [VistaDBInstalledFact]
    public void Probe_savepoint_syntax_variants()
    {
        using var file = new TempVistaDBFile();

        using (var ctx = new ProbeContext(file.ConnectionString))
        {
            ctx.Database.EnsureCreated();
        }

        using var conn = new VistaDBConnection(file.ConnectionString);
        conn.Open();

        Exec(conn, "CREATE TABLE [Probe] ([Id] int NOT NULL IDENTITY(1, 1), [V] int NULL, CONSTRAINT [PK_Probe] PRIMARY KEY ([Id]));");

        // Variant 1: SQL Server-style SAVE TRANSACTION
        TryExec(conn, "SAVE TRANSACTION [sp1];", "SAVE TRANSACTION [sp1]");
        TryExec(conn, "SAVE TRANSACTION sp1;", "SAVE TRANSACTION sp1 (no brackets)");

        // Variant 2: Standard SAVEPOINT
        TryExec(conn, "SAVEPOINT [sp1];", "SAVEPOINT [sp1]");
        TryExec(conn, "SAVEPOINT sp1;", "SAVEPOINT sp1 (no brackets)");

        // Variant 3: With BEGIN TRANSACTION wrapper (savepoints typically require an open transaction)
        _out.WriteLine("");
        _out.WriteLine("--- With outer BEGIN TRAN ---");
        try
        {
            using var tx = conn.BeginTransaction();
            TryExecTx(conn, tx, "SAVE TRANSACTION [sp1];", "SAVE TRANSACTION [sp1] (in tx)");
            TryExecTx(conn, tx, "SAVEPOINT [sp1];", "SAVEPOINT [sp1] (in tx)");
            tx.Rollback();
        }
        catch (Exception ex)
        {
            _out.WriteLine($"  Outer tx setup error: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void Exec(VistaDBConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private void TryExec(VistaDBConnection conn, string sql, string label)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
            _out.WriteLine($"  OK: {label}");
        }
        catch (Exception ex)
        {
            _out.WriteLine($"  FAIL: {label} - {ex.GetType().Name}: {ex.Message.Replace("\n", " | ")}");
        }
    }

    private void TryExecTx(VistaDBConnection conn, System.Data.Common.DbTransaction tx, string sql, string label)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = (global::VistaDB.Provider.VistaDBTransaction)tx;
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
            _out.WriteLine($"  OK: {label}");
        }
        catch (Exception ex)
        {
            _out.WriteLine($"  FAIL: {label} - {ex.GetType().Name}: {ex.Message.Replace("\n", " | ")}");
        }
    }

    private class ProbeContext : DbContext
    {
        private readonly string _cs;
        public ProbeContext(string cs) => _cs = cs;
        protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseVistaDB(_cs);
    }
}
