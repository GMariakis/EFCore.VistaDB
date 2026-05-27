// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Internal;

namespace Microsoft.EntityFrameworkCore.Migrations;

/// <summary>
///     Smoke tests for <c>VistaDBMigrationsSqlGenerator</c>. The behavior we exercise here is the
///     Tier-3 "throw with localized message" path for operations VistaDB cannot express (schemas,
///     sequences). Fully wiring the SQL-emitting Tier-1 path requires the full DI graph, which is
///     covered transitively by the FunctionalTests project at integration time.
/// </summary>
public class VistaDBMigrationsSqlGeneratorTest
{
    [ConditionalFact]
    public void EnsureSchemaOperation_throws_with_SchemasNotSupported_message()
    {
        var generator = CreateGenerator();
        var ex = Assert.Throws<NotSupportedException>(
            () => generator.Generate(new[] { new EnsureSchemaOperation { Name = "custom" } }));

        Assert.Contains("custom", ex.Message);
        Assert.Contains(VistaDBStrings.SchemasNotSupported("custom", "custom"), ex.Message);
    }

    [ConditionalFact]
    public void CreateSequenceOperation_throws_with_SequencesNotSupported_message()
    {
        var generator = CreateGenerator();
        var ex = Assert.Throws<NotSupportedException>(
            () => generator.Generate(new[] { new CreateSequenceOperation { Name = "Foo", ClrType = typeof(int) } }));

        Assert.Equal(VistaDBStrings.SequencesNotSupported, ex.Message);
    }

    [ConditionalFact(Skip = "Pending: build full MigrationsSqlGeneratorDependencies graph for CreateTable/AddColumn/AddForeignKey emission assertions.")]
    public void CreateTableOperation_with_identity_PK_emits_IDENTITY_1_1()
    {
        // TODO(EFCore.VistaDB): assemble MigrationsSqlGeneratorDependencies (type-mapping source,
        // sql-generation helper, update-sql generator, command-builder factory, modification command
        // factory, loggers, current-context, conventions) and assert the emitted SQL contains
        // "IDENTITY(1, 1)" for a CreateTableOperation with an identity-column PK.
    }

    [ConditionalFact(Skip = "Pending: build full MigrationsSqlGeneratorDependencies graph for RenameTableOperation emission assertions.")]
    public void RenameTableOperation_emits_sp_rename()
    {
        // TODO(EFCore.VistaDB): assert the emitted SQL contains "EXEC sp_rename".
    }

    [ConditionalFact(Skip = "Pending: build full MigrationsSqlGeneratorDependencies graph for AddColumnOperation emission assertions.")]
    public void AddColumnOperation_with_nullable_string_emits_alter_table_add()
    {
        // TODO(EFCore.VistaDB): assert "ALTER TABLE ... ADD ... nvarchar".
    }

    [ConditionalFact(Skip = "Pending: build full MigrationsSqlGeneratorDependencies graph; assert AddForeignKeyOperation routes through VistaDBDdaMigrationCommand (Tier-2).")]
    public void AddForeignKeyOperation_with_cascade_produces_VistaDBDdaMigrationCommand()
    {
        // TODO(EFCore.VistaDB): assert the returned IReadOnlyList<MigrationCommand> contains a
        // VistaDBDdaMigrationCommand (not plain MigrationCommand) and that its non-null sync action
        // performs DDA-managed FK cascade behavior.
    }

    /// <summary>
    ///     Builds a generator with the minimum dependencies needed to drive the Tier-3 throw paths.
    /// </summary>
    private static VistaDB.Migrations.VistaDBMigrationsSqlGenerator CreateGenerator()
    {
        // Resolve via the provider's own DI to avoid hand-wiring every dependency.
        var services = new ServiceCollection()
            .AddEntityFrameworkVistaDB()
            .AddDbContext<TestContext>(
                (sp, b) => b
                    .UseInternalServiceProvider(sp)
                    .UseVistaDB("Data Source=test.vdb6"));

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TestContext>();
        return (VistaDB.Migrations.VistaDBMigrationsSqlGenerator)ctx.GetService<IMigrationsSqlGenerator>();
    }

    private class TestContext : DbContext
    {
        public TestContext(DbContextOptions<TestContext> options)
            : base(options)
        {
        }
    }
}
