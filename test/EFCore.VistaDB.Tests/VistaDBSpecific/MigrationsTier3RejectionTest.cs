// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Exercises the Tier-3 ("throw with localized message") dispatch in
///     <c>VistaDBMigrationsSqlGenerator</c> for every migration operation VistaDB cannot express.
///     Complements <c>VistaDBMigrationsSqlGeneratorTest</c> with one assertion per operation.
/// </summary>
public class MigrationsTier3RejectionTest
{
    [ConditionalFact]
    public void EnsureSchemaOperation_custom_throws_NotSupportedException()
        => AssertThrowsNotSupported(new EnsureSchemaOperation { Name = "custom" });

    [ConditionalFact]
    public void EnsureSchemaOperation_message_contains_schema_name()
    {
        var ex = Assert.Throws<NotSupportedException>(
            () => Generator().Generate(new[] { new EnsureSchemaOperation { Name = "MySchema" } }));
        Assert.Contains("MySchema", ex.Message);
    }

    [ConditionalFact]
    public void CreateSequenceOperation_emits_SequencesNotSupported()
    {
        var ex = Assert.Throws<NotSupportedException>(
            () => Generator().Generate(new[] { new CreateSequenceOperation { Name = "S", ClrType = typeof(int) } }));
        Assert.Equal(VistaDBStrings.SequencesNotSupported, ex.Message);
    }

    [ConditionalFact]
    public void AlterSequenceOperation_emits_SequencesNotSupported()
    {
        var ex = Assert.Throws<NotSupportedException>(
            () => Generator().Generate(new[] { new AlterSequenceOperation { Name = "S" } }));
        Assert.Equal(VistaDBStrings.SequencesNotSupported, ex.Message);
    }

    [ConditionalFact]
    public void DropSequenceOperation_emits_SequencesNotSupported()
    {
        var ex = Assert.Throws<NotSupportedException>(
            () => Generator().Generate(new[] { new DropSequenceOperation { Name = "S" } }));
        Assert.Equal(VistaDBStrings.SequencesNotSupported, ex.Message);
    }

    [ConditionalFact]
    public void RestartSequenceOperation_emits_SequencesNotSupported()
    {
        var ex = Assert.Throws<NotSupportedException>(
            () => Generator().Generate(new[] { new RestartSequenceOperation { Name = "S", StartValue = 1 } }));
        Assert.Equal(VistaDBStrings.SequencesNotSupported, ex.Message);
    }

    [ConditionalFact]
    public void RenameSequenceOperation_emits_SequencesNotSupported()
    {
        var ex = Assert.Throws<NotSupportedException>(
            () => Generator().Generate(new[] { new RenameSequenceOperation { Name = "S", NewName = "T" } }));
        Assert.Equal(VistaDBStrings.SequencesNotSupported, ex.Message);
    }

    [ConditionalFact]
    public void RenameTableOperation_with_schema_move_throws_SchemasNotSupported()
    {
        var ex = Assert.Throws<NotSupportedException>(
            () => Generator().Generate(
                new[] { new RenameTableOperation { Name = "T", Schema = "dbo", NewName = "T2", NewSchema = "other" } }));
        Assert.Contains("other", ex.Message);
    }

    [ConditionalFact]
    public void VistaDBCreateDatabaseOperation_type_is_publicly_constructable()
    {
        var op = new VistaDBCreateDatabaseOperation { Name = "MyDb", FileName = "x.vdb6" };
        Assert.Equal("MyDb", op.Name);
        Assert.Equal("x.vdb6", op.FileName);
    }

    [ConditionalFact]
    public void VistaDBDropDatabaseOperation_type_is_publicly_constructable()
    {
        var op = new VistaDBDropDatabaseOperation { Name = "MyDb" };
        Assert.Equal("MyDb", op.Name);
    }

    [ConditionalFact]
    public void All_sequence_operations_share_the_SequencesNotSupported_message()
    {
        var expected = VistaDBStrings.SequencesNotSupported;
        var ops = new MigrationOperation[]
        {
            new CreateSequenceOperation { Name = "S", ClrType = typeof(int) },
            new AlterSequenceOperation { Name = "S" },
            new DropSequenceOperation { Name = "S" },
            new RestartSequenceOperation { Name = "S", StartValue = 1 },
            new RenameSequenceOperation { Name = "S", NewName = "T" }
        };

        foreach (var op in ops)
        {
            var ex = Assert.Throws<NotSupportedException>(
                () => Generator().Generate(new[] { op }));
            Assert.Equal(expected, ex.Message);
        }
    }

    private static void AssertThrowsNotSupported(MigrationOperation op)
        => Assert.Throws<NotSupportedException>(() => Generator().Generate(new[] { op }));

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
