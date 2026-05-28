// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

#nullable disable

public class FieldsOnlyLoadVistaDBTest(FieldsOnlyLoadVistaDBTest.FieldsOnlyLoadVistaDBFixture fixture)
    : FieldsOnlyLoadTestBase<FieldsOnlyLoadVistaDBTest.FieldsOnlyLoadVistaDBFixture>(fixture)
{
    [ConditionalTheory]
    public override void Attached_references_to_principal_are_marked_as_loaded(EntityState state)
        => base.Attached_references_to_principal_are_marked_as_loaded(state);

    [ConditionalTheory]
    public override void Attached_references_to_dependents_are_marked_as_loaded(EntityState state)
        => base.Attached_references_to_dependents_are_marked_as_loaded(state);

    [ConditionalTheory]
    public override void Attached_collections_are_not_marked_as_loaded(EntityState state)
        => base.Attached_collections_are_not_marked_as_loaded(state);

    [ConditionalTheory]
    public override Task Load_collection(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Load_collection(state, queryTrackingBehavior, async);

    [ConditionalTheory]
    public override Task Load_many_to_one_reference_to_principal(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal(state, async);

    [ConditionalTheory]
    public override Task Load_one_to_one_reference_to_principal(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal(state, async);

    [ConditionalTheory]
    public override Task Load_one_to_one_reference_to_dependent(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_dependent(state, async);

    [ConditionalTheory]
    public override Task Load_collection_using_Query(EntityState state, bool async)
        => base.Load_collection_using_Query(state, async);

    [ConditionalTheory]
    public override Task Load_many_to_one_reference_to_principal_using_Query(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_using_Query(state, async);

    [ConditionalTheory]
    public override Task Load_one_to_one_reference_to_principal_using_Query(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_using_Query(state, async);

    public class FieldsOnlyLoadVistaDBFixture : FieldsOnlyLoadFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => VistaDBTestStoreFactory.Instance;
    }
}
