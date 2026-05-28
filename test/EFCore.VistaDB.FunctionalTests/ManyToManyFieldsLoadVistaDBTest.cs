// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestModels.ManyToManyFieldsModel;

namespace Microsoft.EntityFrameworkCore;

#nullable disable

// VistaDB: SQL-baseline overrides dropped. Behavioral checks inherited from spec base.
public class ManyToManyFieldsLoadVistaDBTest(ManyToManyFieldsLoadVistaDBTest.ManyToManyFieldsLoadVistaDBFixture fixture)
    : ManyToManyFieldsLoadTestBase<ManyToManyFieldsLoadVistaDBTest.ManyToManyFieldsLoadVistaDBFixture>(fixture)
{
    [ConditionalTheory]
    public override Task Load_collection(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Load_collection(state, queryTrackingBehavior, async);

    [ConditionalTheory]
    public override Task Load_collection_using_Query(EntityState state, bool async)
        => base.Load_collection_using_Query(state, async);

    [ConditionalTheory]
    public override Task Load_collection_already_loaded(EntityState state, bool async)
        => base.Load_collection_already_loaded(state, async);

    [ConditionalTheory]
    public override Task Load_collection_using_Query_already_loaded(EntityState state, bool async)
        => base.Load_collection_using_Query_already_loaded(state, async);

    [ConditionalTheory]
    public override Task Load_collection_untyped(EntityState state, bool async)
        => base.Load_collection_untyped(state, async);

    [ConditionalTheory]
    public override Task Load_collection_using_Query_untyped(EntityState state, bool async)
        => base.Load_collection_using_Query_untyped(state, async);

    [ConditionalTheory]
    public override Task Load_collection_not_found_untyped(EntityState state, bool async)
        => base.Load_collection_not_found_untyped(state, async);

    [ConditionalTheory]
    public override Task Load_collection_using_Query_not_found_untyped(EntityState state, bool async)
        => base.Load_collection_using_Query_not_found_untyped(state, async);

    public class ManyToManyFieldsLoadVistaDBFixture : ManyToManyFieldsLoadFixtureBase, ITestSqlLoggerFactory
    {
        public TestSqlLoggerFactory TestSqlLoggerFactory
            => (TestSqlLoggerFactory)ListLoggerFactory;

        protected override ITestStoreFactory TestStoreFactory
            => VistaDBTestStoreFactory.Instance;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            modelBuilder
                .Entity<JoinOneSelfPayload>()
                .Property(e => e.Payload)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder
                .SharedTypeEntity<Dictionary<string, object>>("JoinOneToThreePayloadFullShared")
                .IndexerProperty<string>("Payload")
                .HasDefaultValue("Generated");

            modelBuilder
                .Entity<JoinOneToThreePayloadFull>()
                .Property(e => e.Payload)
                .HasDefaultValue("Generated");
        }
    }
}
