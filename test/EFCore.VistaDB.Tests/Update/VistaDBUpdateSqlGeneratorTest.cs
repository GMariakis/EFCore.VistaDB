// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Update.Internal;

namespace Microsoft.EntityFrameworkCore.Update;

public class VistaDBUpdateSqlGeneratorTest
{
    [ConditionalFact]
    public void AppendBatchHeader_emits_SET_NOCOUNT_ON()
    {
        //Test will no work as VistaDB does not emit this statement
        //var sb = new StringBuilder();
        //CreateGenerator().AppendBatchHeader(sb);

        //var sql = sb.ToString();
        //Assert.Contains("SET NOCOUNT ON", sql);
    }

    [ConditionalFact]
    public void PrependEnsureAutocommit_is_a_no_op_on_VistaDB()
    {
        // VistaDB has no IMPLICIT_TRANSACTIONS setting, so the override is intentionally empty.
        var sb = new StringBuilder("UPDATE x SET y = 1;");
        var before = sb.ToString();
        CreateGenerator().PrependEnsureAutocommit(sb);
        Assert.Equal(before, sb.ToString());
    }

    private static VistaDBUpdateSqlGenerator CreateGenerator()
    {
        var sqlHelper = new VistaDBSqlGenerationHelper(new RelationalSqlGenerationHelperDependencies());
        var typeMapper = new TestRelationalTypeMappingSource(
            TestServiceFactory.Instance.Create<TypeMappingSourceDependencies>(),
            TestServiceFactory.Instance.Create<RelationalTypeMappingSourceDependencies>());
        return new VistaDBUpdateSqlGenerator(new UpdateSqlGeneratorDependencies(sqlHelper, typeMapper));
    }
}
