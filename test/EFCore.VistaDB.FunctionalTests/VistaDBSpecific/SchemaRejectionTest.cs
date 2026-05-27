// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     VistaDB: no analog — VistaDB has no concept of schemas other than the implicit default. These tests
///     assert the throw sites that reject any non-<c>dbo</c> schema across the model validator, migrations
///     SQL generator, and SQL generation helper.
/// </summary>
public class SchemaRejectionTest
{
    [ConditionalFact]
    public void DelimitIdentifier_with_null_schema_returns_bracketed_name()
        => Assert.Equal("[Foo]", CreateHelper().DelimitIdentifier("Foo", schema: null));

    [ConditionalFact]
    public void DelimitIdentifier_with_empty_schema_returns_bracketed_name()
        => Assert.Equal("[Foo]", CreateHelper().DelimitIdentifier("Foo", schema: string.Empty));

    [ConditionalFact]
    public void DelimitIdentifier_with_dbo_schema_is_accepted()
        => Assert.Equal("[Foo]", CreateHelper().DelimitIdentifier("Foo", "dbo"));

    [ConditionalFact]
    public void DelimitIdentifier_with_DBO_schema_is_accepted_case_insensitive()
        => Assert.Equal("[Foo]", CreateHelper().DelimitIdentifier("Foo", "DBO"));

    [ConditionalFact]
    public void DelimitIdentifier_with_Dbo_schema_is_accepted_case_insensitive()
        => Assert.Equal("[Foo]", CreateHelper().DelimitIdentifier("Foo", "Dbo"));

    [ConditionalFact]
    public void DelimitIdentifier_with_custom_schema_throws_SchemasNotSupported()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => CreateHelper().DelimitIdentifier("Foo", "custom"));
        Assert.Equal(VistaDBStrings.SchemasNotSupported("Foo", "custom"), ex.Message);
    }

    [ConditionalFact]
    public void DelimitIdentifier_into_builder_with_custom_schema_throws_SchemasNotSupported()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => CreateHelper().DelimitIdentifier(new StringBuilder(), "Foo", "custom"));
        Assert.Equal(VistaDBStrings.SchemasNotSupported("Foo", "custom"), ex.Message);
    }

    [ConditionalFact]
    public void DelimitIdentifier_into_builder_with_dbo_schema_writes_bracketed_name()
    {
        var builder = new StringBuilder();
        CreateHelper().DelimitIdentifier(builder, "Foo", "dbo");
        Assert.Equal("[Foo]", builder.ToString());
    }

    [ConditionalFact]
    public void EnsureSchemaOperation_in_migrations_throws_SchemasNotSupported()
    {
        var generator = CreateMigrationsSqlGenerator();
        var ex = Assert.Throws<NotSupportedException>(
            () => generator.Generate(new[] { new EnsureSchemaOperation { Name = "custom" } }));
        Assert.Contains("custom", ex.Message);
    }

    [ConditionalFact]
    public void SchemasNotSupported_message_contains_both_entity_and_schema_tokens()
    {
        var msg = VistaDBStrings.SchemasNotSupported("MyEntity", "myschema");
        Assert.Contains("MyEntity", msg);
        Assert.Contains("myschema", msg);
    }

    private static VistaDBSqlGenerationHelper CreateHelper()
        => new(new RelationalSqlGenerationHelperDependencies());

    private static VistaDB.Migrations.VistaDBMigrationsSqlGenerator CreateMigrationsSqlGenerator()
    {
        var services = new ServiceCollection()
            .AddEntityFrameworkVistaDB()
            .AddDbContext<TestContext>(
                (sp, b) => b.UseInternalServiceProvider(sp).UseVistaDB("Data Source=test.vdb6"));

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
