// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.EntityFrameworkCore.TestUtilities;
using VistaDB.Provider;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Establishes what VistaDB actually does with <c>@@ROWCOUNT</c>.
///
///     This matters because <c>VistaDBUpdateSqlGenerator</c> emits <c>INSERT …; SELECT @@ROWCOUNT;</c>
///     and EF Core reads that number to decide whether the write landed. If it comes back 0 while the
///     row is in fact inserted, EF throws
///     "expected to affect 1 row(s), but actually affected 0 row(s)" — a concurrency error for a
///     conflict that never happened.
/// </summary>
public class RowCountProbeTest
{
    private readonly ITestOutputHelper _out;

    public RowCountProbeTest(ITestOutputHelper @out)
        => _out = @out;

    private static VistaDBConnection Open(TempVistaDBFile file)
    {
        using (var ctx = new ProbeContext(file.ConnectionString))
        {
            ctx.Database.EnsureCreated();
        }

        var conn = new VistaDBConnection(file.ConnectionString);
        conn.Open();

        using var create = conn.CreateCommand();
        create.CommandText = @"
            CREATE TABLE [Widgets] (
                [Id] uniqueidentifier NOT NULL,
                [Name] nvarchar(100) NULL,
                CONSTRAINT [PK_Widgets] PRIMARY KEY ([Id])
            );";
        create.ExecuteNonQuery();

        return conn;
    }

    [VistaDBInstalledFact]
    public void Is_ROWCOUNT_even_a_supported_expression()
    {
        using var file = new TempVistaDBFile();
        using VistaDBConnection conn = Open(file);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT @@ROWCOUNT;";

        try
        {
            object? value = cmd.ExecuteScalar();
            _out.WriteLine($"SELECT @@ROWCOUNT on its own -> {value ?? "(null)"} ({value?.GetType().Name ?? "null"})");
        }
        catch (Exception ex)
        {
            _out.WriteLine($"*** SELECT @@ROWCOUNT REJECTED: {ex.GetType().Name}: {ex.Message}");
        }
    }

    [VistaDBInstalledFact]
    public void What_does_ROWCOUNT_report_after_an_insert_in_the_same_batch()
    {
        // The exact shape the update generator emits.
        using var file = new TempVistaDBFile();
        using VistaDBConnection conn = Open(file);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO [Widgets] ([Id], [Name]) VALUES (@p0, @p1);
            SELECT @@ROWCOUNT;";
        cmd.Parameters.AddWithValue("@p0", Guid.NewGuid());
        cmd.Parameters.AddWithValue("@p1", "one");

        object? value = cmd.ExecuteScalar();
        _out.WriteLine($"batched INSERT then SELECT @@ROWCOUNT -> {value ?? "(null)"}");

        using var count = conn.CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM [Widgets];";
        _out.WriteLine($"rows actually present -> {count.ExecuteScalar()}");
    }

    [VistaDBInstalledFact]
    public void What_does_ROWCOUNT_report_as_a_separate_command()
    {
        using var file = new TempVistaDBFile();
        using VistaDBConnection conn = Open(file);

        using (var insert = conn.CreateCommand())
        {
            insert.CommandText = "INSERT INTO [Widgets] ([Id], [Name]) VALUES (@p0, @p1);";
            insert.Parameters.AddWithValue("@p0", Guid.NewGuid());
            insert.Parameters.AddWithValue("@p1", "one");
            _out.WriteLine($"ExecuteNonQuery returned -> {insert.ExecuteNonQuery()}");
        }

        using var read = conn.CreateCommand();
        read.CommandText = "SELECT @@ROWCOUNT;";
        try
        {
            _out.WriteLine($"separate SELECT @@ROWCOUNT -> {read.ExecuteScalar() ?? "(null)"}");
        }
        catch (Exception ex)
        {
            _out.WriteLine($"*** separate SELECT @@ROWCOUNT failed: {ex.Message}");
        }
    }

    [VistaDBInstalledFact]
    public void Does_a_reader_surface_the_rowcount_result_set()
    {
        // EF Core consumes this through a reader, not ExecuteScalar, so check that shape too — a
        // result set that is skipped reads as zero rows affected just the same.
        using var file = new TempVistaDBFile();
        using VistaDBConnection conn = Open(file);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO [Widgets] ([Id], [Name]) VALUES (@p0, @p1);
            SELECT @@ROWCOUNT;";
        cmd.Parameters.AddWithValue("@p0", Guid.NewGuid());
        cmd.Parameters.AddWithValue("@p1", "one");

        using VistaDBDataReader reader = cmd.ExecuteReader();

        int resultSets = 0;
        do
        {
            resultSets++;
            _out.WriteLine($"result set {resultSets}: fields={reader.FieldCount}");
            while (reader.Read())
            {
                _out.WriteLine($"  value -> {reader.GetValue(0)} ({reader.GetValue(0)?.GetType().Name})");
            }
        }
        while (reader.NextResult());

        _out.WriteLine($"result sets seen -> {resultSets}");
        _out.WriteLine($"reader.RecordsAffected -> {reader.RecordsAffected}");
    }

    [VistaDBInstalledFact]
    public void What_does_ROWCOUNT_report_after_an_update_that_matches_nothing()
    {
        // The case @@ROWCOUNT exists to catch. If this also reports 1, the check is worthless in the
        // other direction and a genuine concurrency conflict would pass unnoticed.
        using var file = new TempVistaDBFile();
        using VistaDBConnection conn = Open(file);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE [Widgets] SET [Name] = 'x' WHERE [Id] = @p0;
            SELECT @@ROWCOUNT;";
        cmd.Parameters.AddWithValue("@p0", Guid.NewGuid());

        _out.WriteLine($"UPDATE matching no rows -> @@ROWCOUNT = {cmd.ExecuteScalar() ?? "(null)"}");
    }

    [VistaDBInstalledFact]
    public void What_does_ExecuteNonQuery_report_when_nothing_matches()
    {
        // The half that decides whether dropping @@ROWCOUNT is safe: if ExecuteNonQuery cannot
        // distinguish "updated one row" from "matched nothing", EF Core can never see a genuine
        // concurrency conflict.
        using var file = new TempVistaDBFile();
        using VistaDBConnection conn = Open(file);

        using (var seed = conn.CreateCommand())
        {
            seed.CommandText = "INSERT INTO [Widgets] ([Id], [Name]) VALUES (@p0, 'here');";
            seed.Parameters.AddWithValue("@p0", Guid.NewGuid());
            _out.WriteLine($"INSERT one row            -> {seed.ExecuteNonQuery()}");
        }

        using (var hit = conn.CreateCommand())
        {
            hit.CommandText = "UPDATE [Widgets] SET [Name] = 'x';";
            _out.WriteLine($"UPDATE matching one row   -> {hit.ExecuteNonQuery()}");
        }

        using (var miss = conn.CreateCommand())
        {
            miss.CommandText = "UPDATE [Widgets] SET [Name] = 'y' WHERE [Id] = @p0;";
            miss.Parameters.AddWithValue("@p0", Guid.NewGuid());
            _out.WriteLine($"UPDATE matching nothing   -> {miss.ExecuteNonQuery()}");
        }

        using (var missDelete = conn.CreateCommand())
        {
            missDelete.CommandText = "DELETE FROM [Widgets] WHERE [Id] = @p0;";
            missDelete.Parameters.AddWithValue("@p0", Guid.NewGuid());
            _out.WriteLine($"DELETE matching nothing   -> {missDelete.ExecuteNonQuery()}");
        }

        using (var hitDelete = conn.CreateCommand())
        {
            hitDelete.CommandText = "DELETE FROM [Widgets];";
            _out.WriteLine($"DELETE matching one row   -> {hitDelete.ExecuteNonQuery()}");
        }
    }

    [VistaDBInstalledFact]
    public void Does_ExecuteNonQuery_sum_across_a_multi_statement_batch()
    {
        // The 6.6.0 notes record a fix for @@ROWCOUNT *and* ExecuteNonQuery miscounting in a
        // multi-statement batch. EF sends batches, so whether the sum is trustworthy on 6.6.2
        // decides whether the count can be taken from here at all.
        using var file = new TempVistaDBFile();
        using VistaDBConnection conn = Open(file);

        using (var three = conn.CreateCommand())
        {
            three.CommandText = @"
                INSERT INTO [Widgets] ([Id], [Name]) VALUES (@p0, 'a');
                INSERT INTO [Widgets] ([Id], [Name]) VALUES (@p1, 'b');
                INSERT INTO [Widgets] ([Id], [Name]) VALUES (@p2, 'c');";
            three.Parameters.AddWithValue("@p0", Guid.NewGuid());
            three.Parameters.AddWithValue("@p1", Guid.NewGuid());
            three.Parameters.AddWithValue("@p2", Guid.NewGuid());
            _out.WriteLine($"three INSERTs in one batch -> ExecuteNonQuery = {three.ExecuteNonQuery()} (expect 3)");
        }

        using (var count = conn.CreateCommand())
        {
            count.CommandText = "SELECT COUNT(*) FROM [Widgets];";
            _out.WriteLine($"rows actually present      -> {count.ExecuteScalar()}");
        }

        using (var mixed = conn.CreateCommand())
        {
            // One hits, one misses. A correct sum is 1.
            mixed.CommandText = @"
                UPDATE [Widgets] SET [Name] = 'x' WHERE [Name] = 'a';
                UPDATE [Widgets] SET [Name] = 'y' WHERE [Name] = 'nothing-matches-this';";
            _out.WriteLine($"one hit + one miss         -> ExecuteNonQuery = {mixed.ExecuteNonQuery()} (expect 1)");
        }

        using (var allMiss = conn.CreateCommand())
        {
            allMiss.CommandText = @"
                UPDATE [Widgets] SET [Name] = 'z' WHERE [Name] = 'no';
                UPDATE [Widgets] SET [Name] = 'z' WHERE [Name] = 'nope';";
            _out.WriteLine($"two misses                 -> ExecuteNonQuery = {allMiss.ExecuteNonQuery()} (expect 0)");
        }
    }

    private class ProbeContext(string cs) : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseVistaDB(cs);
    }
}
