// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Internal;

// ReSharper disable InconsistentNaming
namespace Microsoft.EntityFrameworkCore.Infrastructure;

public class VistaDBModelValidatorTest : RelationalModelValidatorTest
{
    [ConditionalFact]
    public virtual void Detects_entity_mapped_to_non_default_schema()
    {
        var modelBuilder = CreateConventionModelBuilder();
        modelBuilder.Entity<Pet>().ToTable("Pets", schema: "zoo");

        VerifyError(
            VistaDBStrings.SchemasNotSupported(nameof(Pet), "zoo"),
            modelBuilder);
    }

    [ConditionalFact]
    public virtual void Detects_declared_sequence()
    {
        var modelBuilder = CreateConventionModelBuilder();
        modelBuilder.HasSequence<int>("MySequence");
        modelBuilder.Entity<Pet>();

        VerifyError(VistaDBStrings.SequencesNotSupported, modelBuilder);
    }

    [ConditionalFact]
    public virtual void Detects_property_with_computed_column_sql()
    {
        var modelBuilder = CreateConventionModelBuilder();
        modelBuilder.Entity<Pet>().Property(p => p.Name).HasComputedColumnSql("'computed'");

        VerifyError(
            VistaDBStrings.ComputedColumnsNotSupported(nameof(Pet.Name), nameof(Pet)),
            modelBuilder);
    }

    [ConditionalFact]
    public virtual void Detects_temporal_table_annotation()
    {
        var modelBuilder = CreateConventionModelBuilder();
        modelBuilder.Entity<Pet>().HasAnnotation("SqlServer:IsTemporal", true);

        VerifyError(
            VistaDBStrings.TemporalTablesNotSupported(nameof(Pet)),
            modelBuilder);
    }

    [ConditionalFact]
    public virtual void Detects_memory_optimized_table_annotation()
    {
        var modelBuilder = CreateConventionModelBuilder();
        modelBuilder.Entity<Pet>().HasAnnotation("SqlServer:MemoryOptimized", true);

        VerifyError(VistaDBStrings.MemoryOptimizedTablesNotSupported, modelBuilder);
    }

    [ConditionalFact] // Issue #34324 (relational base, applies to VistaDB)
    public virtual void Throws_for_nested_primitive_collections()
    {
        var modelBuilder = CreateConventionModelBuilder();

        modelBuilder.Entity<WithNestedCollection>(eb =>
        {
            eb.Property(e => e.Id);
            eb.PrimitiveCollection(e => e.SomeStrings);
        });

        VerifyError(
            RelationalStrings.NestedCollectionsNotSupported(
                "string[][]", nameof(WithNestedCollection), nameof(WithNestedCollection.SomeStrings)),
            modelBuilder,
            sensitiveDataLoggingEnabled: false);
    }

    [ConditionalFact]
    public virtual void Passes_for_compatible_decimal_types_within_hierarchy()
    {
        // Relational base scenario: identical decimal store types on shared TPH column should validate.
        var modelBuilder = CreateConventionModelBuilder();
        modelBuilder.Entity<Animal>();
        modelBuilder.Entity<Cat>(cb => cb.Property<decimal>("DecimalCol").HasColumnType("decimal(18,4)").HasColumnName("DecimalCol"));
        modelBuilder.Entity<Dog>(db => db.Property<decimal>("DecimalCol").HasColumnType("decimal(18,4)").HasColumnName("DecimalCol"));

        Validate(modelBuilder);
    }

    // VistaDB: no analog — VistaDB does not currently throw VistaDBStrings.IdentityBadType /
    // DuplicateColumnIdentitySeedMismatch / DuplicateColumnIdentityIncrementMismatch. The SqlServer
    // parallel test methods (Throws_for_identity_on_bad_type / Detects_duplicate_column_names_within_
    // hierarchy_with_different_identity_seed/increment) are dropped here; once VistaDB defines the
    // matching resource strings and validator paths these can be revived.
    // Original SqlServer tests preserved on the SqlServer side for future revival.

    protected class WithNestedCollection
    {
        public int Id { get; set; }
        public string[][] SomeStrings { get; set; }
    }

    private class Pet
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    // VistaDB: no analog — schemas are not supported, so view names never carry a schema prefix.
    // The base test comments out SetViewSchema("Schema") but still expects "Schema.Table" in the
    // error message. Override to expect just "Table".
    [ConditionalFact]
    public override void Detects_duplicate_view_names_without_identifying_relationship()
    {
        var modelBuilder = CreateConventionlessModelBuilder();
        var model = modelBuilder.Model;

        var entityA = model.AddEntityType(typeof(A));
        SetPrimaryKey(entityA);
        AddProperties(entityA);

        var entityB = model.AddEntityType(typeof(B));
        SetPrimaryKey(entityB);
        AddProperties(entityB);
        entityB.AddIgnored(nameof(B.A));
        entityB.AddIgnored(nameof(B.AnotherA));
        entityB.AddIgnored(nameof(B.ManyAs));

        entityA.SetViewName("Table");
        entityB.SetViewName("Table");

        VerifyError(
            RelationalStrings.IncompatibleViewNoRelationship(
                "Table", entityB.DisplayName(), entityA.DisplayName()),
            modelBuilder);
    }

    protected override TestHelpers TestHelpers
        => VistaDBTestHelpers.Instance;
}
