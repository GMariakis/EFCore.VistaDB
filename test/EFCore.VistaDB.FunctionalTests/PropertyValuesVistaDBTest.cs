// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

#nullable disable

public class PropertyValuesVistaDBTest(PropertyValuesVistaDBTest.PropertyValuesVistaDBFixture fixture)
    : PropertyValuesRelationalTestBase<PropertyValuesVistaDBTest.PropertyValuesVistaDBFixture>(fixture)
{
    [ConditionalFact]
    public override Task Scalar_current_values_can_be_accessed_as_a_property_dictionary()
        => base.Scalar_current_values_can_be_accessed_as_a_property_dictionary();

    [ConditionalFact]
    public override Task Scalar_original_values_can_be_accessed_as_a_property_dictionary()
        => base.Scalar_original_values_can_be_accessed_as_a_property_dictionary();

    [ConditionalFact]
    public override Task Scalar_store_values_can_be_accessed_as_a_property_dictionary()
        => base.Scalar_store_values_can_be_accessed_as_a_property_dictionary();

    [ConditionalFact]
    public override Task Scalar_store_values_can_be_accessed_asynchronously_as_a_property_dictionary()
        => base.Scalar_store_values_can_be_accessed_asynchronously_as_a_property_dictionary();

    [ConditionalFact]
    public override Task Scalar_current_values_can_be_accessed_as_a_property_dictionary_using_IProperty()
        => base.Scalar_current_values_can_be_accessed_as_a_property_dictionary_using_IProperty();

    [ConditionalFact]
    public override Task Scalar_original_values_can_be_accessed_as_a_property_dictionary_using_IProperty()
        => base.Scalar_original_values_can_be_accessed_as_a_property_dictionary_using_IProperty();

    [ConditionalFact]
    public override Task Scalar_store_values_can_be_accessed_as_a_property_dictionary_using_IProperty()
        => base.Scalar_store_values_can_be_accessed_as_a_property_dictionary_using_IProperty();

    [ConditionalFact]
    public override Task Scalar_current_values_of_a_derived_object_can_be_accessed_as_a_property_dictionary()
        => base.Scalar_current_values_of_a_derived_object_can_be_accessed_as_a_property_dictionary();

    [ConditionalFact]
    public override Task Scalar_original_values_of_a_derived_object_can_be_accessed_as_a_property_dictionary()
        => base.Scalar_original_values_of_a_derived_object_can_be_accessed_as_a_property_dictionary();

    // The Complex_collection_* / Setting_complex_collection_* / SetValues_throws_for_*_complex_collection_*
    // / Using_complex_property_value_not_list_throws tests query DbSet<School> directly. We Ignore<School>()
    // in OnModelCreating because VistaDB has no JSON column type. The Ignored type can't satisfy
    // context.Set<School>() — EF Core throws "Cannot create a DbSet for 'School' because this type is
    // not included in the model for the context." Skip these tests with a documented reason; the
    // underlying behavior (JSON-column round-trip) is genuinely unsupported on VistaDB.
    private const string JsonNotSupportedSkip
        = "VistaDB: requires JSON column support (School entity has a List<Department> complex-collection property that EF Core maps as a JSON column). VistaDB has no JSON type — School is Ignore<T>()'d at model-creation; tests that query DbSet<School> cannot run.";

    [ConditionalFact(Skip = JsonNotSupportedSkip)]
    public override Task Complex_collection_current_values_can_be_accessed_as_a_property_dictionary()
        => base.Complex_collection_current_values_can_be_accessed_as_a_property_dictionary();

    [ConditionalFact(Skip = JsonNotSupportedSkip)]
    public override Task Complex_collection_original_values_can_be_accessed_as_a_property_dictionary()
        => base.Complex_collection_original_values_can_be_accessed_as_a_property_dictionary();

    [ConditionalFact(Skip = JsonNotSupportedSkip)]
    public override void Setting_complex_collection_values_from_object_works()
        => base.Setting_complex_collection_values_from_object_works();

    [ConditionalFact(Skip = JsonNotSupportedSkip)]
    public override void Setting_complex_collection_current_values_from_object_with_nulls_works()
        => base.Setting_complex_collection_current_values_from_object_with_nulls_works();

    [ConditionalFact(Skip = JsonNotSupportedSkip)]
    public override void Setting_complex_collection_original_values_from_object_with_nulls_works()
        => base.Setting_complex_collection_original_values_from_object_with_nulls_works();

    [ConditionalFact(Skip = JsonNotSupportedSkip)]
    public override void Setting_complex_collection_current_values_from_dictionary_works()
        => base.Setting_complex_collection_current_values_from_dictionary_works();

    [ConditionalFact(Skip = JsonNotSupportedSkip)]
    public override void Setting_complex_collection_values_from_DTO_with_nulls_works()
        => base.Setting_complex_collection_values_from_DTO_with_nulls_works();

    [ConditionalFact(Skip = JsonNotSupportedSkip)]
    public override void Setting_complex_collection_current_values_from_dictionary_with_nulls_works()
        => base.Setting_complex_collection_current_values_from_dictionary_with_nulls_works();

    [ConditionalFact(Skip = JsonNotSupportedSkip)]
    public override void Setting_complex_collection_original_values_from_dictionary_with_nulls_works()
        => base.Setting_complex_collection_original_values_from_dictionary_with_nulls_works();

    [ConditionalFact(Skip = JsonNotSupportedSkip)]
    public override void Setting_complex_collection_current_values_from_DTO_with_complex_metadata_access_works()
        => base.Setting_complex_collection_current_values_from_DTO_with_complex_metadata_access_works();

    [ConditionalFact(Skip = JsonNotSupportedSkip)]
    public override void SetValues_throws_for_complex_collection_with_non_list_value()
        => base.SetValues_throws_for_complex_collection_with_non_list_value();

    [ConditionalFact(Skip = JsonNotSupportedSkip)]
    public override void SetValues_throws_for_complex_collection_with_non_dictionary_item()
        => base.SetValues_throws_for_complex_collection_with_non_dictionary_item();

    [ConditionalFact(Skip = JsonNotSupportedSkip)]
    public override void SetValues_throws_for_nested_complex_collection_with_non_list_value()
        => base.SetValues_throws_for_nested_complex_collection_with_non_list_value();

    [ConditionalFact(Skip = JsonNotSupportedSkip)]
    public override void SetValues_throws_for_nested_complex_collection_with_non_dictionary_item()
        => base.SetValues_throws_for_nested_complex_collection_with_non_dictionary_item();

    [ConditionalFact(Skip = JsonNotSupportedSkip)]
    public override void Using_complex_property_value_not_list_throws()
        => base.Using_complex_property_value_not_list_throws();

    public class PropertyValuesVistaDBFixture : PropertyValuesRelationalFixture
    {
        protected override ITestStoreFactory TestStoreFactory
            => VistaDBTestStoreFactory.Instance;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            modelBuilder.Entity<Building>()
                .Property(b => b.Value).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<CurrentEmployee>()
                .Property(ce => ce.LeaveBalance).HasColumnType("decimal(18,2)");

            // VistaDB: no analog — VistaDB has no JSON column type. The PropertyValuesRelationalFixture
            // includes a `School` entity with a `List<Department>` complex-collection property that EF Core
            // maps as a JSON column by default. Removing it lets the rest of the fixture initialize; the
            // few tests that exercise School directly will fail with "entity not found" and should be
            // marked Skip individually as they surface.
            modelBuilder.Ignore<School>();
        }

        // Mirror the ManyToManyTrackingTestBase pattern: when a fixture explicitly Ignore<T>()s an entity
        // that the spec model maps, EF Core fires MappedEntityTypeIgnoredWarning / MappedPropertyIgnoredWarning
        // / MappedNavigationIgnoredWarning. FixtureBase.AddOptions configures Default → Throw, so these
        // warnings would otherwise crash every test in the class. Suppress them — the Ignore is intentional.
        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => base.AddOptions(builder).ConfigureWarnings(w => w.Ignore(
                CoreEventId.MappedEntityTypeIgnoredWarning,
                CoreEventId.MappedPropertyIgnoredWarning,
                CoreEventId.MappedNavigationIgnoredWarning));
    }
}
