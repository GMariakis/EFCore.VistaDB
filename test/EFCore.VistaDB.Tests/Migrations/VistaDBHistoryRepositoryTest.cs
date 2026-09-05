// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ReSharper disable InconsistentNaming
namespace Microsoft.EntityFrameworkCore.Migrations;

public class VistaDBHistoryRepositoryTest
{
    [ConditionalFact]
    public void GetCreateScript_works()
    {
        var sql = CreateHistoryRepository().GetCreateScript();

        Assert.Equal(
            """
CREATE TABLE [__EFMigrationsHistory] (
    [MigrationId] nvarchar(150) NOT NULL,
    [ProductVersion] nvarchar(32) NOT NULL,
    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
);

""", sql, ignoreLineEndingDifferences: true);
    }

    // VistaDB: no analog — VistaDB does not support schemas.
    // Original SqlServer test (GetCreateScript_works_with_schema, GetCreateIfNotExistsScript_works_with_schema) preserved below for future revival when VistaDB adds support.
    /*
    [ConditionalFact]
    public void GetCreateScript_works_with_schema() { ... }
    [ConditionalFact]
    public void GetCreateIfNotExistsScript_works_with_schema() { ... }
    */

    [ConditionalFact]
    public void GetCreateIfNotExistsScript_works()
    {
        var sql = CreateHistoryRepository().GetCreateIfNotExistsScript();

        // VistaDB has no procedural IF/BEGIN/END and no IF NOT EXISTS, so the condition is evaluated in
        // C# and only the applicable branch is emitted. This context points at a database file that does
        // not exist, so the table is absent and the create is emitted.
        Assert.Contains("-- VistaDB: create of", sql);
        Assert.Contains("CREATE TABLE [__EFMigrationsHistory]", sql);
    }

    [ConditionalFact]
    public void GetDeleteScript_works()
    {
        var sql = CreateHistoryRepository().GetDeleteScript("Migration1");

        Assert.Equal(
            """
DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] = N'Migration1';

""", sql, ignoreLineEndingDifferences: true);
    }

    [ConditionalFact]
    public void GetInsertScript_works()
    {
        var sql = CreateHistoryRepository().GetInsertScript(
            new HistoryRow("Migration1", "7.0.0"));

        Assert.Equal(
            """
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'Migration1', N'7.0.0');

""", sql, ignoreLineEndingDifferences: true);
    }

    [ConditionalFact]
    public void GetBeginIfNotExistsScript_emits_VistaDB_comment_marker()
    {
        // VistaDB has no IF/BEGIN/END SQL — the override emits a comment marker instead.
        var sql = CreateHistoryRepository().GetBeginIfNotExistsScript("Migration1");
        Assert.Contains("VistaDB: BEGIN IF NOT EXISTS", sql);
        Assert.Contains("Migration1", sql);
    }

    [ConditionalFact]
    public void GetBeginIfExistsScript_emits_VistaDB_comment_marker()
    {
        var sql = CreateHistoryRepository().GetBeginIfExistsScript("Migration1");
        Assert.Contains("VistaDB: BEGIN IF EXISTS", sql);
        Assert.Contains("Migration1", sql);
    }

    [ConditionalFact]
    public void GetEndIfScript_works()
    {
        var sql = CreateHistoryRepository().GetEndIfScript();
        Assert.Contains("VistaDB: END IF", sql);
    }

    [ConditionalFact]
    public void Create_script_carries_no_SqlServer_catalog_probe()
    {
        // VistaDB has neither OBJECT_ID/sys.tables nor the SqlServer-style INFORMATION_SCHEMA views
        // (querying those fails with error 627), so nothing of that shape may appear in the emitted SQL.
        var repo = (Microsoft.EntityFrameworkCore.VistaDB.Migrations.Internal.VistaDBHistoryRepository)CreateHistoryRepository();
        var createIfNotExists = repo.GetCreateIfNotExistsScript();

        Assert.DoesNotContain("OBJECT_ID", createIfNotExists);
        Assert.DoesNotContain("sys.tables", createIfNotExists);
        Assert.DoesNotContain("INFORMATION_SCHEMA", createIfNotExists);
    }

    [ConditionalFact]
    public void Exists_is_false_when_the_database_file_is_absent()
    {
        // The probe short-circuits on a missing file rather than letting the engine raise. This is what
        // makes the very first startup work, before any database has been created.
        var repo = (Microsoft.EntityFrameworkCore.VistaDB.Migrations.Internal.VistaDBHistoryRepository)CreateHistoryRepository();

        Assert.False(repo.Exists());
    }

    [ConditionalFact]
    public void ExistsSql_is_not_supported()
    {
        // Existence is probed in C# instead; the base class still requires the member, so it throws
        // rather than silently emitting SQL VistaDB would reject.
        var repo = (Microsoft.EntityFrameworkCore.VistaDB.Migrations.Internal.VistaDBHistoryRepository)CreateHistoryRepository();
        var existsSql = typeof(HistoryRepository).GetProperty(
            "ExistsSql", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() => existsSql!.GetValue(repo));
        Assert.IsType<NotSupportedException>(ex.InnerException);
    }

    // VistaDB: no analog — VistaDB has no sp_getapplock / sp_releaseapplock; AcquireDatabaseLock returns an inert lock.
    // Original SqlServer applock-shape test preserved below for future revival when VistaDB adds support.
    /*
    [ConditionalFact]
    public void AcquireDatabaseLock_uses_sp_getapplock() { ... }
    */

    private static IHistoryRepository CreateHistoryRepository()
        => new TestDbContext(
                new DbContextOptionsBuilder()
                    .UseInternalServiceProvider(VistaDBFixture.DefaultServiceProvider)
                    .UseVistaDB(
                        "Data Source=Dummy.vdb6",
                        b => b.MigrationsHistoryTable(HistoryRepository.DefaultTableName))
                    .Options)
            .GetService<IHistoryRepository>();

    private class TestDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Blog> Blogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
    }

    private class Blog
    {
        public int Id { get; set; }
    }
}
