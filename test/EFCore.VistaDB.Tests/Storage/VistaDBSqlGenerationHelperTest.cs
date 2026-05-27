// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

namespace Microsoft.EntityFrameworkCore.Storage;

public class VistaDBSqlGenerationHelperTest
{
    [ConditionalFact]
    public void DelimitIdentifier_wraps_identifier_in_square_brackets()
        => Assert.Equal("[Foo]", CreateHelper().DelimitIdentifier("Foo"));

    [ConditionalFact]
    public void DelimitIdentifier_with_dbo_schema_collapses_to_no_schema()
        => Assert.Equal("[Foo]", CreateHelper().DelimitIdentifier("Foo", "dbo"));

    [ConditionalFact]
    public void DelimitIdentifier_with_non_dbo_schema_throws_VistaDB_schemas_not_supported()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => CreateHelper().DelimitIdentifier("Foo", "custom"));
        Assert.Contains("custom", ex.Message);
        Assert.Contains("schemas", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [ConditionalFact]
    public void StartTransactionStatement_emits_BEGIN_TRANSACTION()
        => Assert.StartsWith("BEGIN TRANSACTION", CreateHelper().StartTransactionStatement);

    [ConditionalFact]
    public void EscapeIdentifier_doubles_closing_bracket()
        => Assert.Equal("Foo]]Bar", CreateHelper().EscapeIdentifier("Foo]Bar"));

    [ConditionalFact]
    public void BatchTerminator_uses_relational_default_NewLine()
    {
        // VistaDB does not override BatchTerminator — unlike SqlServer which emits "GO\r\n\r\n",
        // VistaDB falls back to the relational base default of Environment.NewLine.
        Assert.Equal(Environment.NewLine, CreateHelper().BatchTerminator);
    }

    [ConditionalFact]
    public void GenerateCreateSavepointStatement_emits_SAVE_TRANSACTION()
    {
        var sql = CreateHelper().GenerateCreateSavepointStatement("MySavepoint");
        Assert.Contains("SAVE TRANSACTION", sql);
        Assert.Contains("[MySavepoint]", sql);
    }

    [ConditionalFact]
    public void GenerateRollbackToSavepointStatement_emits_ROLLBACK_TRANSACTION()
    {
        var sql = CreateHelper().GenerateRollbackToSavepointStatement("MySavepoint");
        Assert.Contains("ROLLBACK TRANSACTION", sql);
        Assert.Contains("[MySavepoint]", sql);
    }

    private static VistaDBSqlGenerationHelper CreateHelper()
        => new(new RelationalSqlGenerationHelperDependencies());
}
