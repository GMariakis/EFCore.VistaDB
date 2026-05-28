// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestModels.ManyToManyModel;

namespace Microsoft.EntityFrameworkCore;

#nullable disable

public abstract class ManyToManyTrackingVistaDBTestBase<TFixture>(TFixture fixture)
    : ManyToManyTrackingRelationalTestBase<TFixture>(fixture)
    where TFixture : ManyToManyTrackingVistaDBTestBase<TFixture>.ManyToManyTrackingVistaDBFixtureBase
{
    [ConditionalTheory]
    public override Task Can_insert_many_to_many_composite_with_navs(bool async)
        => base.Can_insert_many_to_many_composite_with_navs(async);

    [ConditionalFact]
    public override Task Can_update_many_to_many_composite_with_navs()
        => base.Can_update_many_to_many_composite_with_navs();

    [ConditionalFact]
    public override Task Can_delete_with_many_to_many_composite_with_navs()
        => base.Can_delete_with_many_to_many_composite_with_navs();

    [ConditionalTheory]
    public override Task Can_insert_many_to_many_composite_shared_with_navs(bool async)
        => base.Can_insert_many_to_many_composite_shared_with_navs(async);

    [ConditionalFact]
    public override Task Can_update_many_to_many_composite_shared_with_navs()
        => base.Can_update_many_to_many_composite_shared_with_navs();

    [ConditionalTheory]
    public override Task Can_insert_many_to_many_with_navs(bool async)
        => base.Can_insert_many_to_many_with_navs(async);

    [ConditionalFact]
    public override Task Can_update_many_to_many_with_navs()
        => base.Can_update_many_to_many_with_navs();

    [ConditionalTheory]
    public override Task Can_insert_many_to_many_with_inheritance(bool async)
        => base.Can_insert_many_to_many_with_inheritance(async);

    [ConditionalTheory]
    public override Task Can_insert_many_to_many(bool async)
        => base.Can_insert_many_to_many(async);


    // VistaDBOnDeleteConvention (ported from SqlServerOnDeleteConvention) converts Cascade ->
    // ClientCascade for self-referencing skip-navigation FKs that share a join table. The expected
    // entries here match SqlServer's behavior on the same model.
    protected override Dictionary<string, DeleteBehavior> CustomDeleteBehaviors { get; } = new()
    {
        { "EntityBranch.RootSkipShared", DeleteBehavior.ClientCascade },
        { "EntityBranch2.Leaf2SkipShared", DeleteBehavior.ClientCascade },
        { "EntityBranch2.SelfSkipSharedLeft", DeleteBehavior.ClientCascade },
        { "EntityOne.SelfSkipPayloadLeft", DeleteBehavior.ClientCascade },
        { "EntityTableSharing1.TableSharing2Shared", DeleteBehavior.ClientCascade },
        { "EntityTwo.SelfSkipSharedLeft", DeleteBehavior.ClientCascade },
        { "UnidirectionalEntityBranch.UnidirectionalEntityRoot", DeleteBehavior.ClientCascade },
        { "UnidirectionalEntityOne.SelfSkipPayloadLeft", DeleteBehavior.ClientCascade },
        { "UnidirectionalEntityTwo.SelfSkipSharedRight", DeleteBehavior.ClientCascade },
    };

    public class ManyToManyTrackingVistaDBFixtureBase : ManyToManyTrackingRelationalFixture, ITestSqlLoggerFactory
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

            modelBuilder
                .Entity<UnidirectionalJoinOneSelfPayload>()
                .Property(e => e.Payload)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder
                .SharedTypeEntity<Dictionary<string, object>>("UnidirectionalJoinOneToThreePayloadFullShared")
                .IndexerProperty<string>("Payload")
                .HasDefaultValue("Generated");

            modelBuilder
                .Entity<UnidirectionalJoinOneToThreePayloadFull>()
                .Property(e => e.Payload)
                .HasDefaultValue("Generated");
        }
    }
}
