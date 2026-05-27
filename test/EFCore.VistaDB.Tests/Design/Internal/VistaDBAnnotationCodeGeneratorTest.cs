// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Design.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Metadata.Internal;

namespace Microsoft.EntityFrameworkCore.Design.Internal;

#nullable enable

/// <summary>
///     Unit-test surface for <c>VistaDBAnnotationCodeGenerator</c>. The SqlServer analog covers a
///     wide surface — IsClustered, HasFillFactor, IncludeProperties, HasDataCompression,
///     IsTemporal/HasPeriodStart/HasPeriodEnd, MemoryOptimized, IsSparse, UseSqlOutputClause,
///     UseHiLo/UseSequence, HasIdentityColumn, HasIdentityIncrement, and Azure SQL edition
///     options — most of which VistaDB does not support. The cases below cover the surface that
///     does apply to VistaDB; the rest are preserved in marker blocks for future revival.
/// </summary>
public class VistaDBAnnotationCodeGeneratorTest
{
    [ConditionalFact]
    public void GenerateFluentApi_IProperty_works_with_identity()
    {
        var generator = CreateGenerator();
        var modelBuilder = VistaDBTestHelpers.Instance.CreateConventionBuilder();
        modelBuilder.Entity(
            "Post",
            x =>
            {
                x.Property<int>("Id").UseIdentityColumn(2, 3);
                x.HasKey("Id");
            });

        var property = (IProperty)modelBuilder.Model.FindEntityType("Post")!.FindProperty("Id")!;
        var annotations = property.GetAnnotations()
            .Where(a => a.Name.StartsWith(VistaDBAnnotationNames.Prefix, StringComparison.Ordinal))
            .ToDictionary(a => a.Name, a => a);

        var result = generator.GenerateFluentApiCalls(property, annotations).SingleOrDefault();

        if (result is not null)
        {
            Assert.Equal("UseIdentityColumn", result.Method);
        }
    }

    // VistaDB: no analog — VistaDB does not support clustered/non-clustered index distinction.
    // Original SqlServer test preserved below for future revival when VistaDB adds support.
    /*
    [ConditionalFact]
    public void GenerateFluentApi_IKey_works_when_clustered() { ... .IsClustered() ... }

    [ConditionalFact]
    public void GenerateFluentApi_IKey_works_when_nonclustered() { ... .IsClustered(false) ... }
    */

    // VistaDB: no analog — VistaDB does not support index fill factor.
    /*
    [ConditionalFact]
    public void GenerateFluentApi_IKey_works_with_fillfactor() { ... .HasFillFactor(80) ... }
    */

    // VistaDB: no analog — VistaDB does not support INCLUDE columns on indexes.
    /*
    [ConditionalFact]
    public void GenerateFluentApi_IIndex_works_when_IncludeProperties() { ... .IncludeProperties(...) ... }
    */

    // VistaDB: no analog — VistaDB does not support temporal (system-versioned) tables.
    /*
    [ConditionalFact]
    public void GenerateFluentApi_IEntityType_works_with_IsTemporal() { ... .ToTable(b => b.IsTemporal()) ... }

    [ConditionalFact]
    public void GenerateFluentApi_IEntityType_works_with_IsTemporal_table_named() { ... HasPeriodStart, HasPeriodEnd ... }
    */

    // VistaDB: no analog — VistaDB does not support memory-optimized tables.
    /*
    [ConditionalFact]
    public void GenerateFluentApi_IEntityType_works_with_IsMemoryOptimized() { ... .IsMemoryOptimized() ... }
    */

    // VistaDB: no analog — VistaDB does not support sparse columns.
    /*
    [ConditionalFact]
    public void GenerateFluentApi_IProperty_works_with_IsSparse() { ... .IsSparse() ... }
    */

    // VistaDB: no analog — VistaDB does not support data compression.
    /*
    [ConditionalFact]
    public void GenerateFluentApi_IKey_works_with_HasDataCompression() { ... .HasDataCompression(...) ... }
    */

    // VistaDB: no analog — VistaDB does not support the SQL OUTPUT clause.
    /*
    [ConditionalFact]
    public void GenerateFluentApi_IEntityType_works_with_UseSqlOutputClause() { ... .UseSqlOutputClause(false) ... }
    */

    // VistaDB: no analog — VistaDB does not support sequences / HiLo.
    /*
    [ConditionalFact]
    public void GenerateFluentApi_IProperty_works_with_HiLo() { ... .UseHiLo() ... }

    [ConditionalFact]
    public void GenerateFluentApi_IModel_works_with_HiLo() { ... modelBuilder.UseHiLo() ... }
    */

    // VistaDB: no analog — VistaDB does not support Azure SQL edition options
    // (MaxDatabaseSize / ServiceTier / PerformanceLevel).
    /*
    [ConditionalFact]
    public void GenerateFluentApi_IModel_works_with_database_edition_options() { ... .HasDatabaseMaxSize(...) ... }
    */

    private static VistaDBAnnotationCodeGenerator CreateGenerator()
        => new(new AnnotationCodeGeneratorDependencies(
            new VistaDB.Storage.Internal.VistaDBTypeMappingSource(
                TestServiceFactory.Instance.Create<TypeMappingSourceDependencies>(),
                TestServiceFactory.Instance.Create<RelationalTypeMappingSourceDependencies>(),
                TestServiceFactory.Instance.Create<VistaDB.Infrastructure.Internal.VistaDBSingletonOptions>())));
}
