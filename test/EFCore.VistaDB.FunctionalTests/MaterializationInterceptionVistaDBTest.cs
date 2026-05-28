// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

public class MaterializationInterceptionVistaDBTest(NonSharedFixture fixture) :
    MaterializationInterceptionTestBase<MaterializationInterceptionVistaDBTest.VistaDBLibraryContext>(fixture)
{
    [ConditionalTheory]
    public override Task Binding_interceptors_are_used_by_queries(bool inject, bool usePooling)
        => base.Binding_interceptors_are_used_by_queries(inject, usePooling);

    [ConditionalTheory]
    public override Task Binding_interceptors_are_used_when_creating_instances(bool inject, bool usePooling)
        => base.Binding_interceptors_are_used_when_creating_instances(inject, usePooling);

    [ConditionalTheory]
    public override Task Intercept_query_materialization_for_empty_constructor(bool inject, bool usePooling)
        => base.Intercept_query_materialization_for_empty_constructor(inject, usePooling);

    [ConditionalTheory]
    public override Task Intercept_query_materialization_for_full_constructor(bool inject, bool usePooling)
        => base.Intercept_query_materialization_for_full_constructor(inject, usePooling);

    [ConditionalTheory]
    public override Task Multiple_materialization_interceptors_can_be_used(bool inject, bool usePooling)
        => base.Multiple_materialization_interceptors_can_be_used(inject, usePooling);

    // These tests query DbSet<TestEntity30244> which the spec base maps as an owned-JSON entity.
    // VistaDB has no JSON column type, so VistaDBLibraryContext.OnModelCreating omits the entity
    // (the OwnsMany.ToJson() line is commented out under the no-analog convention). EF Core then
    // throws "The entity type 'TestEntity30244' was not found." Skip with reference to JSON gap.
    [ConditionalTheory(Skip = "VistaDB: requires JSON column support — TestEntity30244 uses OwnsMany.ToJson() which VistaDB has no analog for.")]
    public override Task Intercept_query_materialization_with_owned_types(bool async, bool usePooling)
        => base.Intercept_query_materialization_with_owned_types(async, usePooling);

    [ConditionalTheory(Skip = "VistaDB: requires JSON column support — TestEntity30244 uses OwnsMany.ToJson() which VistaDB has no analog for.")]
    public override Task Intercept_query_materialization_with_owned_types_projecting_collection(bool async, bool usePooling)
        => base.Intercept_query_materialization_with_owned_types_projecting_collection(async, usePooling);

    public class VistaDBLibraryContext(DbContextOptions options) : LibraryContext(options)
    {
        // VistaDB: no analog — VistaDB has no structural JSON column type; we omit the owned-JSON model fragment.
        /*
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TestEntity30244>().OwnsMany(e => e.Settings, b => b.ToJson());
        }
        */
    }

    protected override ITestStoreFactory TestStoreFactory
        => VistaDBTestStoreFactory.Instance;
}
