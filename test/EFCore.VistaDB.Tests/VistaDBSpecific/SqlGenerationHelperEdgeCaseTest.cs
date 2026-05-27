// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Edge-case unit tests for <see cref="VistaDBSqlGenerationHelper"/>. These complement
///     <c>VistaDBSqlGenerationHelperTest</c> by exercising identifier escaping under unusual inputs and
///     the schema-rejection paths from multiple angles.
/// </summary>
public class SqlGenerationHelperEdgeCaseTest
{
    [ConditionalFact]
    public void DelimitIdentifier_with_simple_name_returns_bracketed()
        => Assert.Equal("[Foo]", Create().DelimitIdentifier("Foo"));

    [ConditionalFact]
    public void DelimitIdentifier_with_underscore_name_is_preserved()
        => Assert.Equal("[_my_table]", Create().DelimitIdentifier("_my_table"));

    [ConditionalFact]
    public void DelimitIdentifier_with_digits_is_preserved()
        => Assert.Equal("[Table42]", Create().DelimitIdentifier("Table42"));

    [ConditionalFact]
    public void DelimitIdentifier_with_inner_bracket_escapes_it()
        => Assert.Equal("[Foo]]Bar]", Create().DelimitIdentifier("Foo]Bar"));

    [ConditionalFact]
    public void DelimitIdentifier_with_two_inner_brackets_escapes_both()
        => Assert.Equal("[a]]b]]c]", Create().DelimitIdentifier("a]b]c"));

    [ConditionalFact]
    public void EscapeIdentifier_with_no_brackets_returns_same_string()
        => Assert.Equal("Plain", Create().EscapeIdentifier("Plain"));

    [ConditionalFact]
    public void EscapeIdentifier_into_builder_writes_doubled_bracket()
    {
        var sb = new StringBuilder();
        Create().EscapeIdentifier(sb, "Foo]Bar");
        Assert.Equal("Foo]]Bar", sb.ToString());
    }

    [ConditionalFact]
    public void DelimitIdentifier_into_builder_writes_full_bracketed_form()
    {
        var sb = new StringBuilder();
        Create().DelimitIdentifier(sb, "Foo");
        Assert.Equal("[Foo]", sb.ToString());
    }

    [ConditionalFact]
    public void StatementTerminator_is_present()
        => Assert.False(string.IsNullOrEmpty(Create().StatementTerminator));

    [ConditionalFact]
    public void StartTransactionStatement_ends_with_statement_terminator()
    {
        var helper = Create();
        Assert.EndsWith(helper.StatementTerminator, helper.StartTransactionStatement);
    }

    [ConditionalFact]
    public void GenerateCreateSavepointStatement_contains_delimited_name()
    {
        var sql = Create().GenerateCreateSavepointStatement("sp1");
        Assert.Contains("[sp1]", sql);
    }

    [ConditionalFact]
    public void GenerateRollbackToSavepointStatement_contains_delimited_name()
    {
        var sql = Create().GenerateRollbackToSavepointStatement("sp1");
        Assert.Contains("[sp1]", sql);
    }

    [ConditionalFact]
    public void DelimitIdentifier_with_DBO_schema_uppercase_is_accepted()
        => Assert.Equal("[Foo]", Create().DelimitIdentifier("Foo", "DBO"));

    [ConditionalFact]
    public void DelimitIdentifier_with_Custom_schema_throws_with_schema_in_message()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => Create().DelimitIdentifier("Foo", "MySchema"));
        Assert.Contains("MySchema", ex.Message);
    }

    [ConditionalFact]
    public void DelimitIdentifier_with_empty_schema_returns_simple_bracketed_form()
        => Assert.Equal("[Foo]", Create().DelimitIdentifier("Foo", string.Empty));

    [ConditionalFact]
    public void DelimitIdentifier_into_builder_with_DBO_schema_writes_simple_form()
    {
        var sb = new StringBuilder();
        Create().DelimitIdentifier(sb, "Foo", "dbo");
        Assert.Equal("[Foo]", sb.ToString());
    }

    [ConditionalFact]
    public void Two_helper_instances_produce_identical_output_for_same_input()
    {
        var a = Create();
        var b = Create();
        Assert.Equal(a.DelimitIdentifier("X"), b.DelimitIdentifier("X"));
        Assert.Equal(a.StartTransactionStatement, b.StartTransactionStatement);
    }

    private static VistaDBSqlGenerationHelper Create()
        => new(new RelationalSqlGenerationHelperDependencies());
}
