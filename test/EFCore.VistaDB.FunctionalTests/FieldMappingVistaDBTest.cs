// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

#nullable disable

public class FieldMappingVistaDBTest(FieldMappingVistaDBTest.FieldMappingVistaDBFixture fixture)
    : FieldMappingTestBase<FieldMappingVistaDBTest.FieldMappingVistaDBFixture>(fixture)
{
    protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        => facade.UseTransaction(transaction.GetDbTransaction());

    [ConditionalFact]
    public override void Field_mapping_with_conversion_does_not_throw()
        => base.Field_mapping_with_conversion_does_not_throw();

    [ConditionalTheory]
    public override void Simple_query_auto_props(bool tracking) => base.Simple_query_auto_props(tracking);

    [ConditionalTheory]
    public override void Include_collection_auto_props(bool tracking) => base.Include_collection_auto_props(tracking);

    [ConditionalTheory]
    public override void Include_reference_auto_props(bool tracking) => base.Include_reference_auto_props(tracking);

    [ConditionalFact]
    public override void Load_collection_auto_props() => base.Load_collection_auto_props();

    [ConditionalFact]
    public override void Load_reference_auto_props() => base.Load_reference_auto_props();

    [ConditionalTheory]
    public override void Query_with_conditional_constant_auto_props(bool tracking)
        => base.Query_with_conditional_constant_auto_props(tracking);

    [ConditionalTheory]
    public override void Query_with_conditional_param_auto_props(bool tracking)
        => base.Query_with_conditional_param_auto_props(tracking);

    [ConditionalTheory]
    public override void Projection_auto_props(bool tracking) => base.Projection_auto_props(tracking);

    [ConditionalFact]
    public override Task Update_auto_props() => base.Update_auto_props();

    public class FieldMappingVistaDBFixture : FieldMappingFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => VistaDBTestStoreFactory.Instance;
    }
}
