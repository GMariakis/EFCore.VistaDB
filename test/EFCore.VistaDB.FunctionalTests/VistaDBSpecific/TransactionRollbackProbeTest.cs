// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.EntityFrameworkCore.TestUtilities;
using VistaDB.Provider;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Probes whether VistaDB transactions actually roll back INSERT/UPDATE/DELETE operations.
///     If they don't, the entire GraphUpdates spec test pattern
///     (<c>ExecuteWithStrategyInTransactionAsync</c>) is broken because tests assume rollback
///     restores the seeded state between iterations.
/// </summary>
public class TransactionRollbackProbeTest
{
    private readonly ITestOutputHelper _out;

    public TransactionRollbackProbeTest(ITestOutputHelper @out)
        => _out = @out;

    [VistaDBInstalledFact]
    public void Rollback_undoes_delete()
    {
        using var file = new TempVistaDBFile();
        using (var ctx = new ProbeContext(file.ConnectionString))
        {
            ctx.Database.EnsureCreated();
        }

        using var conn = new VistaDBConnection(file.ConnectionString);
        conn.Open();

        Exec(conn, "CREATE TABLE [Probe] ([Id] int NOT NULL IDENTITY(1, 1), [V] int NULL, CONSTRAINT [PK_Probe] PRIMARY KEY ([Id]));");
        Exec(conn, "INSERT INTO [Probe] ([V]) VALUES (100);");

        // Confirm row inserted.
        _out.WriteLine($"Initial count: {CountRows(conn)}");

        // BEGIN tx, DELETE, then ROLLBACK
        using (var tx = conn.BeginTransaction())
        {
            ExecTx(conn, tx, "DELETE FROM [Probe] WHERE [V] = 100;");
            _out.WriteLine($"Inside tx after DELETE: {CountRowsTx(conn, tx)}");
            tx.Rollback();
        }

        var afterRollback = CountRows(conn);
        _out.WriteLine($"After rollback: {afterRollback}");
        _out.WriteLine(afterRollback == 1 ? "PASS — DELETE was rolled back, row restored" : "FAIL — row was NOT restored");
        Assert.Equal(1, afterRollback);
    }

    [VistaDBInstalledFact]
    public void Dispose_without_commit_rolls_back_delete()
    {
        using var file = new TempVistaDBFile();
        using (var ctx = new ProbeContext(file.ConnectionString))
        {
            ctx.Database.EnsureCreated();
        }

        using var conn = new VistaDBConnection(file.ConnectionString);
        conn.Open();

        Exec(conn, "CREATE TABLE [Probe] ([Id] int NOT NULL IDENTITY(1, 1), [V] int NULL, CONSTRAINT [PK_Probe] PRIMARY KEY ([Id]));");
        Exec(conn, "INSERT INTO [Probe] ([V]) VALUES (100);");
        _out.WriteLine($"Initial count: {CountRows(conn)}");

        // KEY DIFFERENCE: dispose without calling Rollback() — what EF Core does via `using` blocks.
        using (var tx = conn.BeginTransaction())
        {
            ExecTx(conn, tx, "DELETE FROM [Probe] WHERE [V] = 100;");
            _out.WriteLine($"Inside tx after DELETE: {CountRowsTx(conn, tx)}");
            // No explicit Rollback() — rely on `using` to dispose.
        }

        var afterDispose = CountRows(conn);
        _out.WriteLine($"After dispose-without-commit: {afterDispose}");
        _out.WriteLine(afterDispose == 1 ? "PASS — Dispose-without-Commit rolled back the DELETE" : "FAIL — DELETE persisted past Dispose");
        Assert.Equal(1, afterDispose);
    }

    [VistaDBInstalledFact]
    public void Repeated_transactions_isolate_changes()
    {
        // Mirror EF Core's GraphUpdates ExecuteWithStrategyInTransactionAsync pattern: a shared
        // VistaDBConnection across multiple test iterations, each iteration wraps work in a fresh
        // BeginTransaction/Dispose-without-commit pair. If VistaDB allows iteration 1's DELETE to
        // leak past Dispose despite no Commit, iteration 2 sees the row gone and fails with
        // @@ROWCOUNT = 0 → DbUpdateConcurrencyException.
        using var file = new TempVistaDBFile();
        using (var ctx = new ProbeContext(file.ConnectionString))
        {
            ctx.Database.EnsureCreated();
        }

        // Shared connection across iterations.
        using var conn = new VistaDBConnection(file.ConnectionString);
        conn.Open();
        Exec(conn, "CREATE TABLE [Probe] ([Id] int NOT NULL IDENTITY(1, 1), [V] int NULL, CONSTRAINT [PK_Probe] PRIMARY KEY ([Id]));");
        Exec(conn, "INSERT INTO [Probe] ([V]) VALUES (100);");

        for (var i = 1; i <= 4; i++)
        {
            using var tx = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = (global::VistaDB.Provider.VistaDBTransaction)tx;
            cmd.CommandText = "DELETE FROM [Probe] WHERE [V] = 100; SELECT @@ROWCOUNT;";
            var affected = Convert.ToInt32(cmd.ExecuteScalar());
            _out.WriteLine($"Iteration {i}: DELETE affected {affected} row(s)");
            // No Commit — `using` should rollback.
        }

        var finalCount = CountRows(conn);
        _out.WriteLine($"Final count: {finalCount} (expected 1)");
        Assert.Equal(1, finalCount);
    }

    [VistaDBInstalledFact]
    public void Rollback_undoes_insert()
    {
        using var file = new TempVistaDBFile();
        using (var ctx = new ProbeContext(file.ConnectionString))
        {
            ctx.Database.EnsureCreated();
        }

        using var conn = new VistaDBConnection(file.ConnectionString);
        conn.Open();

        Exec(conn, "CREATE TABLE [Probe] ([Id] int NOT NULL IDENTITY(1, 1), [V] int NULL, CONSTRAINT [PK_Probe] PRIMARY KEY ([Id]));");

        _out.WriteLine($"Initial count: {CountRows(conn)}");

        using (var tx = conn.BeginTransaction())
        {
            ExecTx(conn, tx, "INSERT INTO [Probe] ([V]) VALUES (200);");
            _out.WriteLine($"Inside tx after INSERT: {CountRowsTx(conn, tx)}");
            tx.Rollback();
        }

        var afterRollback = CountRows(conn);
        _out.WriteLine($"After rollback: {afterRollback}");
        _out.WriteLine(afterRollback == 0 ? "PASS — INSERT was rolled back" : "FAIL — INSERTed row persisted");
        Assert.Equal(0, afterRollback);
    }

    private static void Exec(VistaDBConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static void ExecTx(VistaDBConnection conn, System.Data.Common.DbTransaction tx, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = (global::VistaDB.Provider.VistaDBTransaction)tx;
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static int CountRows(VistaDBConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM [Probe]";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static int CountRowsTx(VistaDBConnection conn, System.Data.Common.DbTransaction tx)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = (global::VistaDB.Provider.VistaDBTransaction)tx;
        cmd.CommandText = "SELECT COUNT(*) FROM [Probe]";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private class ProbeContext : DbContext
    {
        private readonly string _cs;
        public ProbeContext(string cs) => _cs = cs;
        protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseVistaDB(_cs);
    }
}
