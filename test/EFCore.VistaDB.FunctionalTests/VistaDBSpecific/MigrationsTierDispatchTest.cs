// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Asserts the correct tier dispatch for each migration operation against the VistaDB Migrations
///     SQL generator. Tier 1 = native SQL, Tier 2 = DDA, Tier 3 = throw <c>NotSupportedException</c> with
///     a localized <see cref="VistaDBStrings"/> message. Live DDA execution is not exercised here.
/// </summary>
public class MigrationsTierDispatchTest
{
    [ConditionalFact]
    public void EnsureSchemaOperation_with_custom_schema_routes_to_Tier3_throw()
    {
        var ex = Assert.Throws<NotSupportedException>(
            () => Generator().Generate(new[] { new EnsureSchemaOperation { Name = "custom" } }));
        Assert.Contains("custom", ex.Message);
    }

    [ConditionalFact]
    public void CreateSequenceOperation_routes_to_Tier3_throw_with_SequencesNotSupported()
    {
        var ex = Assert.Throws<NotSupportedException>(
            () => Generator().Generate(new[] { new CreateSequenceOperation { Name = "S", ClrType = typeof(int) } }));
        Assert.Equal(VistaDBStrings.SequencesNotSupported, ex.Message);
    }

    [ConditionalFact]
    public void AlterSequenceOperation_routes_to_Tier3_throw_with_SequencesNotSupported()
    {
        var ex = Assert.Throws<NotSupportedException>(
            () => Generator().Generate(new[] { new AlterSequenceOperation { Name = "S" } }));
        Assert.Equal(VistaDBStrings.SequencesNotSupported, ex.Message);
    }

    [ConditionalFact]
    public void DropSequenceOperation_routes_to_Tier3_throw_with_SequencesNotSupported()
    {
        var ex = Assert.Throws<NotSupportedException>(
            () => Generator().Generate(new[] { new DropSequenceOperation { Name = "S" } }));
        Assert.Equal(VistaDBStrings.SequencesNotSupported, ex.Message);
    }

    [ConditionalFact]
    public void RestartSequenceOperation_routes_to_Tier3_throw_with_SequencesNotSupported()
    {
        var ex = Assert.Throws<NotSupportedException>(
            () => Generator().Generate(new[] { new RestartSequenceOperation { Name = "S", StartValue = 1 } }));
        Assert.Equal(VistaDBStrings.SequencesNotSupported, ex.Message);
    }

    [ConditionalFact]
    public void RenameSequenceOperation_routes_to_Tier3_throw_with_SequencesNotSupported()
    {
        var ex = Assert.Throws<NotSupportedException>(
            () => Generator().Generate(new[] { new RenameSequenceOperation { Name = "S", NewName = "T" } }));
        Assert.Equal(VistaDBStrings.SequencesNotSupported, ex.Message);
    }

    [ConditionalFact]
    public void RenameTableOperation_with_schema_change_throws_SchemasNotSupported()
    {
        // VistaDB: cross-schema move is rejected because schemas are unsupported.
        var ex = Assert.Throws<NotSupportedException>(
            () => Generator().Generate(
                new[] { new RenameTableOperation { Name = "T", Schema = "dbo", NewName = "T2", NewSchema = "other" } }));
        Assert.Contains("other", ex.Message);
    }

    [ConditionalFact]
    public void Sequences_all_share_the_same_SequencesNotSupported_message()
    {
        // VistaDB: defensive — keep all sequence Tier-3 paths in sync on the same string.
        var msg = VistaDBStrings.SequencesNotSupported;
        Assert.Equal(
            msg,
            Assert.Throws<NotSupportedException>(
                () => Generator().Generate(new[] { new CreateSequenceOperation { Name = "S", ClrType = typeof(int) } })).Message);
        Assert.Equal(
            msg,
            Assert.Throws<NotSupportedException>(
                () => Generator().Generate(new[] { new DropSequenceOperation { Name = "S" } })).Message);
    }

    [ConditionalFact]
    public void VistaDBCreateDatabaseOperation_type_is_resolvable()
    {
        // VistaDB: Tier-2 file-level operation. Must exist as a publicly-typed migration operation.
        var op = new VistaDBCreateDatabaseOperation { Name = "Test" };
        Assert.Equal("Test", op.Name);
    }

    [ConditionalFact]
    public void VistaDBDropDatabaseOperation_type_is_resolvable()
    {
        var op = new VistaDBDropDatabaseOperation { Name = "Test" };
        Assert.Equal("Test", op.Name);
    }

    [ConditionalFact]
    public void Generator_resolves_to_VistaDBMigrationsSqlGenerator_type()
    {
        var g = Generator();
        Assert.IsType<VistaDB.Migrations.VistaDBMigrationsSqlGenerator>(g);
    }

    private static IMigrationsSqlGenerator Generator()
    {
        var services = new ServiceCollection()
            .AddEntityFrameworkVistaDB()
            .AddDbContext<TestContext>(
                (sp, b) => b.UseInternalServiceProvider(sp).UseVistaDB("Data Source=test.vdb6"));

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TestContext>();
        return ctx.GetService<IMigrationsSqlGenerator>();
    }

    private class TestContext : DbContext
    {
        public TestContext(DbContextOptions<TestContext> options)
            : base(options)
        {
        }
    }
}
