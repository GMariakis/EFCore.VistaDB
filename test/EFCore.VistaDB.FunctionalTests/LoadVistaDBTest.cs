// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

#nullable disable

// VistaDB: LoadSqlServerTest is ~1800 lines, dominated by SQL baseline overrides. We inherit the
// behavioral checks only; SQL-baseline overrides are dropped because VistaDB produces different SQL.
public class LoadVistaDBTest : LoadTestBase<LoadVistaDBTest.LoadVistaDBFixture>
{
    public LoadVistaDBTest(LoadVistaDBFixture fixture)
        : base(fixture)
        => fixture.TestSqlLoggerFactory.Clear();

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
    public override Task Load_one_to_one_PK_to_PK_reference_to_principal(EntityState state, bool async)
        => base.Load_one_to_one_PK_to_PK_reference_to_principal(state, async);

    [ConditionalTheory]
    public override Task Load_one_to_one_PK_to_PK_reference_to_dependent(EntityState state, bool async)
        => base.Load_one_to_one_PK_to_PK_reference_to_dependent(state, async);

    [ConditionalTheory]
    public override Task Load_collection_using_Query(EntityState state, bool async)
        => base.Load_collection_using_Query(state, async);

    [ConditionalTheory]
    public override Task Load_many_to_one_reference_to_principal_using_Query(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_using_Query(state, async);

    [ConditionalTheory]
    public override Task Load_one_to_one_reference_to_principal_using_Query(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_using_Query(state, async);

    [ConditionalTheory]
    public override Task Load_one_to_one_reference_to_dependent_using_Query(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_dependent_using_Query(state, async);

    public class LoadVistaDBFixture : LoadFixtureBase
    {
        public TestSqlLoggerFactory TestSqlLoggerFactory
            => (TestSqlLoggerFactory)ListLoggerFactory;

        protected override ITestStoreFactory TestStoreFactory
            => VistaDBTestStoreFactory.Instance;
    }
}
