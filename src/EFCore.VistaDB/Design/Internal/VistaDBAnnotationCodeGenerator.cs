// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.EntityFrameworkCore.VistaDB.Metadata.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDB.Design.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
/// <remarks>
///     VistaDB-specific annotation code generator. The annotation surface is a small subset of SqlServer's:
///     VistaDB has no clustered/non-clustered distinction, no fill factor, no included columns, no temporal
///     tables, no memory-optimized tables, no HiLo/Sequence value-generation strategies, no Azure SQL edition
///     options, no sparse columns, no data compression, no sort-in-tempdb, no key sequences and no MaxDatabaseSize.
///     Only identity column emission and the VistaDB value-generation strategy survive.
/// </remarks>
public class VistaDBAnnotationCodeGenerator : AnnotationCodeGenerator
{
    private static readonly MethodInfo PropertyUseIdentityColumnsMethodInfo
        = typeof(VistaDBPropertyBuilderExtensions).GetRuntimeMethod(
            nameof(VistaDBPropertyBuilderExtensions.UseIdentityColumn),
            [typeof(PropertyBuilder), typeof(long), typeof(int)])!;

    // VistaDB: no analog — the SqlServer generator emits a comprehensive set of fluent-API method handles
    // (UseHiLo, UseKeySequences, HasDatabaseMaxSize, IsClustered, IsTemporal, IsMemoryOptimized, IsSparse,
    // HasFillFactor, IncludeProperties, UseDataCompression, SortInTempDb, temporal period APIs …).
    // Original SqlServer MethodInfo registrations preserved below for future revival as VistaDB grows analogs.
    /*
    private static readonly MethodInfo ModelUseIdentityColumnsMethodInfo = ...;
    private static readonly MethodInfo ModelUseHiLoMethodInfo = ...;
    private static readonly MethodInfo ModelUseKeySequencesMethodInfo = ...;
    private static readonly MethodInfo ModelHasDatabaseMaxSizeMethodInfo = ...;
    private static readonly MethodInfo ModelHasServiceTierSqlMethodInfo = ...;
    private static readonly MethodInfo ModelHasPerformanceLevelSqlMethodInfo = ...;
    private static readonly MethodInfo EntityTypeIsMemoryOptimizedMethodInfo = ...;
    private static readonly MethodInfo PropertyIsSparseMethodInfo = ...;
    private static readonly MethodInfo PropertyUseHiLoMethodInfo = ...;
    private static readonly MethodInfo PropertyUseSequenceMethodInfo = ...;
    private static readonly MethodInfo IndexIsClusteredMethodInfo = ...;
    private static readonly MethodInfo IndexIncludePropertiesMethodInfo = ...;
    private static readonly MethodInfo IndexHasFillFactorMethodInfo = ...;
    private static readonly MethodInfo IndexSortInTempDbMethodInfo = ...;
    private static readonly MethodInfo IndexUseDataCompressionMethodInfo = ...;
    private static readonly MethodInfo KeyIsClusteredMethodInfo = ...;
    private static readonly MethodInfo KeyHasFillFactorMethodInfo = ...;
    private static readonly MethodInfo TableIsTemporalMethodInfo = ...;
    private static readonly MethodInfo TemporalTableUseHistoryTableMethodInfo1 = ...;
    private static readonly MethodInfo TemporalTableUseHistoryTableMethodInfo2 = ...;
    private static readonly MethodInfo TemporalTableHasPeriodStartMethodInfo = ...;
    private static readonly MethodInfo TemporalTableHasPeriodEndMethodInfo = ...;
    private static readonly MethodInfo TemporalPropertyHasColumnNameMethodInfo = ...;
    */

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBAnnotationCodeGenerator(AnnotationCodeGeneratorDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        IProperty property,
        IDictionary<string, IAnnotation> annotations)
    {
        var fragments = new List<MethodCallCodeFragment>(base.GenerateFluentApiCalls(property, annotations));

        if (GenerateValueGenerationStrategy(annotations) is { } valueGenerationStrategy)
        {
            fragments.Add(valueGenerationStrategy);
        }

        // VistaDB: no analog — no sparse columns. Original SqlServer logic preserved below.
        /*
        if (GetAndRemove<bool?>(annotations, SqlServerAnnotationNames.Sparse) is { } isSparse)
        {
            fragments.Add(isSparse ? new MethodCallCodeFragment(PropertyIsSparseMethodInfo)
                                   : new MethodCallCodeFragment(PropertyIsSparseMethodInfo, false));
        }
        */

        return fragments;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override bool IsHandledByConvention(IModel model, IAnnotation annotation)
    {
        if (annotation.Name == RelationalAnnotationNames.DefaultSchema)
        {
            // VistaDB only has the implicit "dbo" namespace — treat it as the default and don't emit it.
            return (string?)annotation.Value == "dbo";
        }

        // VistaDB-specific: the default identity strategy is implicit at the model level.
        return annotation.Name == VistaDBAnnotationNames.ValueGenerationStrategy
            && (VistaDBValueGenerationStrategy)annotation.Value! == VistaDBValueGenerationStrategy.IdentityColumn;
    }

    private static MethodCallCodeFragment? GenerateValueGenerationStrategy(IDictionary<string, IAnnotation> annotations)
    {
        if (!annotations.TryGetValue(VistaDBAnnotationNames.ValueGenerationStrategy, out var strategyAnnotation)
            || strategyAnnotation.Value is null)
        {
            return null;
        }

        annotations.Remove(VistaDBAnnotationNames.ValueGenerationStrategy);
        var strategy = (VistaDBValueGenerationStrategy)strategyAnnotation.Value;

        switch (strategy)
        {
            case VistaDBValueGenerationStrategy.IdentityColumn:
            {
                long seed = 1L;
                if (annotations.TryGetValue(VistaDBAnnotationNames.IdentitySeed, out var seedAnnotation)
                    && seedAnnotation.Value is { } seedValue)
                {
                    annotations.Remove(VistaDBAnnotationNames.IdentitySeed);
                    seed = seedValue is int intSeed ? intSeed : (long)seedValue;
                }

                var increment = 1;
                if (annotations.TryGetValue(VistaDBAnnotationNames.IdentityIncrement, out var incrementAnnotation)
                    && incrementAnnotation.Value is { } incrementValue)
                {
                    annotations.Remove(VistaDBAnnotationNames.IdentityIncrement);
                    increment = Convert.ToInt32(incrementValue, CultureInfo.InvariantCulture);
                }
                return new MethodCallCodeFragment(
                    PropertyUseIdentityColumnsMethodInfo,
                    (seed, increment) switch
                    {
                        (1L, 1) => [],
                        (_, 1) => [seed],
                        _ => [seed, increment]
                    });
            }

            // VistaDB: no analog — VistaDB has no CREATE SEQUENCE, so the HiLo and Sequence strategies
            // are not available. Original SqlServer cases preserved for future revival.
            /*
            case SqlServerValueGenerationStrategy.SequenceHiLo: ...
            case SqlServerValueGenerationStrategy.Sequence: ...
            */

            case VistaDBValueGenerationStrategy.None:
            default:
                return null;
        }
    }

}
