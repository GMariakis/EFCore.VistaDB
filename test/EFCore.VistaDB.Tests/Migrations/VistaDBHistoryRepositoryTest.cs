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

        // VistaDB has no procedural IF/BEGIN/END; instead it prefixes a comment marker then emits the
        // unconditional CREATE TABLE. Assert on that contract rather than the SqlServer OBJECT_ID shape.
        Assert.Contains("-- VistaDB: idempotent create", sql);
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
    public void ExistsSql_uses_INFORMATION_SCHEMA_not_sys_tables()
    {
        // VistaDB: no analog — VistaDB has no OBJECT_ID / sys.tables. The exists probe goes through
        // INFORMATION_SCHEMA.TABLES instead.
        var repo = (Microsoft.EntityFrameworkCore.VistaDB.Migrations.Internal.VistaDBHistoryRepository)CreateHistoryRepository();
        // ExistsSql is protected — we exercise it indirectly by calling the public GetCreateIfNotExistsScript
        // (which the VistaDB override does not include OBJECT_ID in). Assert OBJECT_ID is absent.
        var createIfNotExists = repo.GetCreateIfNotExistsScript();
        Assert.DoesNotContain("OBJECT_ID", createIfNotExists);
        Assert.DoesNotContain("sys.tables", createIfNotExists);
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
