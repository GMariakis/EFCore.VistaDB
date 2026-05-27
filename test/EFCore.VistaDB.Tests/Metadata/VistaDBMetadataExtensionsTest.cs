// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Metadata.Internal;

namespace Microsoft.EntityFrameworkCore.Metadata;

/// <summary>
///     Unit-test surface for VistaDB-specific metadata getters/setters on <c>IProperty</c>,
///     <c>IEntityType</c>, <c>IIndex</c>, <c>IKey</c>, and <c>IModel</c>. The SqlServer parallel
///     (<c>SqlServerMetadataExtensionsTest</c>, ~498 lines) exercises schema getters, HiLo/sequence
///     accessors, identity seed/increment, clustered/fill-factor/include accessors, temporal/memory-
///     optimized accessors, and Azure-edition options.
///
///     VistaDB does not support schemas, sequences, clustered/non-clustered indexes, fill factor,
///     include columns, temporal tables, memory-optimized tables, OUTPUT clause, or Azure edition
///     options. The supported accessors are identity-related; those round-trip through the
///     <c>VistaDBAnnotationNames</c> constants directly.
/// </summary>
public class VistaDBMetadataExtensionsTest
{
    [ConditionalFact]
    public void Can_round_trip_identity_seed_via_annotation()
    {
        var modelBuilder = VistaDBTestHelpers.Instance.CreateConventionBuilder();
        modelBuilder.Entity<Customer>().Property(e => e.Id).UseIdentityColumn(seed: 7, increment: 3);

        var model = modelBuilder.Model.FinalizeModel();
        var property = model.FindEntityType(typeof(Customer))!.FindProperty(nameof(Customer.Id))!;

        Assert.Equal(7L, property.FindAnnotation(VistaDBAnnotationNames.IdentitySeed)!.Value);
        Assert.Equal(3, property.FindAnnotation(VistaDBAnnotationNames.IdentityIncrement)!.Value);
    }

    [ConditionalFact]
    public void Default_max_identifier_length_is_128()
    {
        var modelBuilder = VistaDBTestHelpers.Instance.CreateConventionBuilder();
        modelBuilder.Entity<Customer>();
        var model = modelBuilder.Model.FinalizeModel();

        Assert.Equal(128, model.GetMaxIdentifierLength());
    }

    // VistaDB: no analog — VistaDB does not support schemas. There is no GetDefaultSchema()
    // round-trip via VistaDBModelExtensions because VistaDBModelExtensions does not exist.
    // Original SqlServer tests preserved below for future revival when VistaDB adds support.
    /*
    [ConditionalFact]
    public void Can_get_and_set_default_schema() { ... model.SetDefaultSchema("custom") ... }

    [ConditionalFact]
    public void Can_get_and_set_HiLo_sequence_name() { ... property.SetHiLoSequenceName(...) ... }

    [ConditionalFact]
    public void Can_get_and_set_HiLo_sequence_schema() { ... property.SetHiLoSequenceSchema(...) ... }

    [ConditionalFact]
    public void Can_get_and_set_clustered() { ... key.SetIsClustered(true) ... }

    [ConditionalFact]
    public void Can_get_and_set_fill_factor() { ... index.SetFillFactor(80) ... }

    [ConditionalFact]
    public void Can_get_and_set_include_properties() { ... index.SetIncludeProperties(...) ... }

    [ConditionalFact]
    public void Can_get_and_set_is_temporal() { ... entityType.SetIsTemporal(true) ... }

    [ConditionalFact]
    public void Can_get_and_set_is_memory_optimized() { ... entityType.SetIsMemoryOptimized(true) ... }

    [ConditionalFact]
    public void Can_get_and_set_use_sql_output_clause() { ... entityType.SetUseSqlOutputClause(false) ... }
    */

    private class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
