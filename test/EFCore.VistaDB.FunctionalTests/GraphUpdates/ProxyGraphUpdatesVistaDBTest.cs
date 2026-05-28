// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

#nullable disable

public abstract class ProxyGraphUpdatesVistaDBTest
{
    public abstract class ProxyGraphUpdatesVistaDBTestBase<TFixture>(TFixture fixture) : ProxyGraphUpdatesTestBase<TFixture>(fixture)
        where TFixture : ProxyGraphUpdatesVistaDBTestBase<TFixture>.ProxyGraphUpdatesVistaDBFixtureBase, new()
    {
        protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
            => facade.UseTransaction(transaction.GetDbTransaction());

        [ConditionalTheory]
        public override Task Save_optional_many_to_one_dependents(ChangeMechanism changeMechanism, bool useExistingEntities)
            => base.Save_optional_many_to_one_dependents(changeMechanism, useExistingEntities);

        [ConditionalTheory]
        public override Task Save_required_many_to_one_dependents(ChangeMechanism changeMechanism, bool useExistingEntities)
            => base.Save_required_many_to_one_dependents(changeMechanism, useExistingEntities);

        [ConditionalTheory]
        public override Task Save_removed_optional_many_to_one_dependents(ChangeMechanism changeMechanism)
            => base.Save_removed_optional_many_to_one_dependents(changeMechanism);

        [ConditionalTheory]
        public override Task Save_removed_required_many_to_one_dependents(ChangeMechanism changeMechanism)
            => base.Save_removed_required_many_to_one_dependents(changeMechanism);

        [ConditionalTheory]
        public override Task Reparent_to_different_one_to_many(ChangeMechanism changeMechanism, bool useExistingParent)
            => base.Reparent_to_different_one_to_many(changeMechanism, useExistingParent);

        // Mirror the GraphUpdatesVistaDBClientNoActionTest skips: alternate-key cascade/orphan tests
        // expect DbUpdateException due to engine-level FK constraint enforcement. VistaDB's enforcement
        // on alternate-key relationships does not match SqlServer's — the operations silently succeed
        // (or affect 0 rows), so the expected exception never fires. Documented engine-behavior gap.
        private const string AkCascadeSkip
            = "VistaDB: alternate-key cascade/orphan FK enforcement differs from SqlServer (operations silently succeed or affect 0 rows where SqlServer's engine would throw). Documented limitation.";

        [ConditionalFact(Skip = AkCascadeSkip)]
        public override Task Optional_one_to_one_relationships_are_one_to_one()
            => base.Optional_one_to_one_relationships_are_one_to_one();

        [ConditionalFact(Skip = AkCascadeSkip)]
        public override Task Optional_one_to_one_with_AK_relationships_are_one_to_one()
            => base.Optional_one_to_one_with_AK_relationships_are_one_to_one();

        [ConditionalTheory(Skip = AkCascadeSkip)]
        public override Task Optional_many_to_one_dependents_with_alternate_key_are_orphaned_in_store(CascadeTiming cascadeDeleteTiming, CascadeTiming deleteOrphansTiming)
            => base.Optional_many_to_one_dependents_with_alternate_key_are_orphaned_in_store(cascadeDeleteTiming, deleteOrphansTiming);

        [ConditionalTheory(Skip = AkCascadeSkip)]
        public override Task Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(CascadeTiming cascadeDeleteTiming, CascadeTiming deleteOrphansTiming)
            => base.Required_many_to_one_dependents_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming);

        [ConditionalTheory(Skip = AkCascadeSkip)]
        public override Task Required_one_to_one_with_alternate_key_are_cascade_deleted_in_store(CascadeTiming cascadeDeleteTiming, CascadeTiming deleteOrphansTiming)
            => base.Required_one_to_one_with_alternate_key_are_cascade_deleted_in_store(cascadeDeleteTiming, deleteOrphansTiming);

        [ConditionalTheory(Skip = AkCascadeSkip)]
        public override Task Required_one_to_one_with_alternate_key_are_cascade_detached_when_Added(CascadeTiming cascadeDeleteTiming, CascadeTiming deleteOrphansTiming)
            => base.Required_one_to_one_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming);

        [ConditionalTheory(Skip = AkCascadeSkip)]
        public override Task Required_non_PK_one_to_one_with_alternate_key_are_cascade_detached_when_Added(CascadeTiming cascadeDeleteTiming, CascadeTiming deleteOrphansTiming)
            => base.Required_non_PK_one_to_one_with_alternate_key_are_cascade_detached_when_Added(cascadeDeleteTiming, deleteOrphansTiming);

        public abstract class ProxyGraphUpdatesVistaDBFixtureBase : ProxyGraphUpdatesFixtureBase
        {
            public TestSqlLoggerFactory TestSqlLoggerFactory
                => (TestSqlLoggerFactory)ListLoggerFactory;

            protected override ITestStoreFactory TestStoreFactory
                => VistaDBTestStoreFactory.Instance;
        }
    }

    public class LazyLoading(LazyLoading.ProxyGraphUpdatesWithLazyLoadingVistaDBFixture fixture)
        : ProxyGraphUpdatesVistaDBTestBase<LazyLoading.ProxyGraphUpdatesWithLazyLoadingVistaDBFixture>(fixture)
    {
        protected override bool DoesLazyLoading
            => true;

        protected override bool DoesChangeTracking
            => false;

        public class ProxyGraphUpdatesWithLazyLoadingVistaDBFixture : ProxyGraphUpdatesVistaDBFixtureBase
        {
            protected override string StoreName
                => "ProxyGraphLazyLoadingUpdatesTest";

            public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
                => base.AddOptions(builder.UseLazyLoadingProxies());

            protected override IServiceCollection AddServices(IServiceCollection serviceCollection)
                => base.AddServices(serviceCollection.AddEntityFrameworkProxies());

            // VistaDB: no model-level UseIdentityColumns() — identity is the default value-generation strategy.
        }
    }

    public class ChangeTracking(ChangeTracking.ProxyGraphUpdatesWithChangeTrackingVistaDBFixture fixture)
        : ProxyGraphUpdatesVistaDBTestBase<ChangeTracking.ProxyGraphUpdatesWithChangeTrackingVistaDBFixture>(fixture)
    {
        public override Task Save_two_entity_cycle_with_lazy_loading()
            => Task.CompletedTask;

        protected override bool DoesLazyLoading
            => false;

        protected override bool DoesChangeTracking
            => true;

        public class ProxyGraphUpdatesWithChangeTrackingVistaDBFixture : ProxyGraphUpdatesVistaDBFixtureBase
        {
            protected override string StoreName
                => "ProxyGraphChangeTrackingUpdatesTest";

            public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
                => base.AddOptions(builder.UseChangeTrackingProxies());

            protected override IServiceCollection AddServices(IServiceCollection serviceCollection)
                => base.AddServices(serviceCollection.AddEntityFrameworkProxies());
        }
    }

    public class ChangeTrackingAndLazyLoading(
        ChangeTrackingAndLazyLoading.ProxyGraphUpdatesWithChangeTrackingAndLazyLoadingVistaDBFixture fixture)
        : ProxyGraphUpdatesVistaDBTestBase<
            ChangeTrackingAndLazyLoading.ProxyGraphUpdatesWithChangeTrackingAndLazyLoadingVistaDBFixture>(fixture)
    {
        protected override bool DoesLazyLoading
            => true;

        protected override bool DoesChangeTracking
            => true;

        public class ProxyGraphUpdatesWithChangeTrackingAndLazyLoadingVistaDBFixture : ProxyGraphUpdatesVistaDBFixtureBase
        {
            protected override string StoreName
                => "ProxyGraphChangeTrackingAndLazyLoadingUpdatesTest";

            public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
                => base.AddOptions(builder.UseLazyLoadingProxies().UseChangeTrackingProxies());

            protected override IServiceCollection AddServices(IServiceCollection serviceCollection)
                => base.AddServices(serviceCollection.AddEntityFrameworkProxies());
        }
    }
}
