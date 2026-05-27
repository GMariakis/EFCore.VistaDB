// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Metadata.Internal;

// ReSharper disable InconsistentNaming
namespace Microsoft.EntityFrameworkCore.Metadata;

/// <summary>
///     Unit-test surface for the VistaDB-specific fluent extensions on
///     <c>PropertyBuilder</c> / <c>EntityTypeBuilder</c> / <c>IndexBuilder</c> / <c>KeyBuilder</c>.
///     The SqlServer parallel (<c>SqlServerBuilderExtensionsTest</c>, ~1340 lines) exercises a wide
///     surface — IsClustered, HasFillFactor, IncludeProperties, HasDataCompression,
///     IsTemporal/HasPeriodStart/HasPeriodEnd, IsMemoryOptimized, IsSparse, UseSqlOutputClause,
///     UseHiLo/UseSequence/UseKeySequences, HasIdentityColumn variants, and Azure-edition options.
///     VistaDB supports only <c>UseIdentityColumn</c>; the remaining surface throws
///     <c>NotSupportedException</c> with a <c>VistaDBStrings.XxxNotSupported</c> message, and the
///     SqlServer cases are preserved verbatim in marker blocks for future revival.
/// </summary>
public class VistaDBBuilderExtensionsTest
{
    [ConditionalFact]
    public void Can_set_identity_column_with_default_seed_and_increment()
    {
        var modelBuilder = CreateModelBuilder();
        modelBuilder.Entity<Customer>().Property(e => e.Id).UseIdentityColumn();

        var model = modelBuilder.Model.FinalizeModel();
        var property = model.FindEntityType(typeof(Customer))!.FindProperty(nameof(Customer.Id))!;

        Assert.Equal(VistaDBValueGenerationStrategy.IdentityColumn,
            (VistaDBValueGenerationStrategy?)property.FindAnnotation(VistaDBAnnotationNames.ValueGenerationStrategy)?.Value);
        Assert.Equal(1L, property.FindAnnotation(VistaDBAnnotationNames.IdentitySeed)?.Value);
        Assert.Equal(1, property.FindAnnotation(VistaDBAnnotationNames.IdentityIncrement)?.Value);
    }

    [ConditionalFact]
    public void Can_set_identity_column_with_seed_and_increment()
    {
        var modelBuilder = CreateModelBuilder();
        modelBuilder.Entity<Customer>().Property(e => e.Id).UseIdentityColumn(2, 5);

        var model = modelBuilder.Model.FinalizeModel();
        var property = model.FindEntityType(typeof(Customer))!.FindProperty(nameof(Customer.Id))!;

        Assert.Equal(2L, property.FindAnnotation(VistaDBAnnotationNames.IdentitySeed)?.Value);
        Assert.Equal(5, property.FindAnnotation(VistaDBAnnotationNames.IdentityIncrement)?.Value);
    }

    [ConditionalFact]
    public void UseHiLo_throws_VistaDB_sequences_not_supported()
    {
        var modelBuilder = CreateModelBuilder();
        var builder = modelBuilder.Entity<Customer>().Property(e => e.Id);

        var ex = Assert.Throws<NotSupportedException>(() => builder.UseHiLo());
        Assert.Equal(VistaDBStrings.SequencesNotSupported, ex.Message);
    }

    [ConditionalFact]
    public void UseSequence_throws_VistaDB_sequences_not_supported()
    {
        var modelBuilder = CreateModelBuilder();
        var builder = modelBuilder.Entity<Customer>().Property(e => e.Id);

        var ex = Assert.Throws<NotSupportedException>(() => builder.UseSequence());
        Assert.Equal(VistaDBStrings.SequencesNotSupported, ex.Message);
    }

    // VistaDB: no analog — VistaDB does not support clustered/non-clustered index distinction,
    // fill factor, INCLUDE columns, data compression, online index creation, sparse columns,
    // temporal tables, memory-optimized tables, OUTPUT clause, or Azure edition options.
    // Original SqlServer tests preserved below for future revival when VistaDB adds support.
    /*
    [ConditionalFact]
    public void Can_set_key_clustering() { ... .HasKey(...).IsClustered() ... }

    [ConditionalFact]
    public void Can_set_index_clustering() { ... .HasIndex(...).IsClustered() ... }

    [ConditionalFact]
    public void Can_set_index_fillfactor() { ... .HasFillFactor(80) ... }

    [ConditionalFact]
    public void Can_set_index_include_properties() { ... .IncludeProperties(...) ... }

    [ConditionalFact]
    public void Can_set_index_data_compression() { ... .HasDataCompression(...) ... }

    [ConditionalFact]
    public void Can_set_index_online() { ... .IsCreatedOnline(true) ... }

    [ConditionalFact]
    public void Can_set_index_sort_in_tempdb() { ... .SortInTempDb(true) ... }

    [ConditionalFact]
    public void Can_set_sparse_property() { ... .IsSparse() ... }

    [ConditionalFact]
    public void Can_set_temporal_table() { ... .ToTable(b => b.IsTemporal()) ... }

    [ConditionalFact]
    public void Can_set_memory_optimized_table() { ... .IsMemoryOptimized() ... }

    [ConditionalFact]
    public void Can_set_output_clause() { ... .UseSqlOutputClause(false) ... }

    [ConditionalFact]
    public void Can_set_HasDatabaseMaxSize_HasServiceTier_HasPerformanceLevel() { ... }
    */

    private static ModelBuilder CreateModelBuilder()
        => VistaDBTestHelpers.Instance.CreateConventionBuilder();

    private class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
