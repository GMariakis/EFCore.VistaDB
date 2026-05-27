// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Metadata.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDB.Metadata.Conventions;

/// <summary>
///     A convention that configures store value generation as <see cref="ValueGenerated.OnAdd" /> on properties that are
///     part of the primary key and not part of any foreign keys, were configured to have a database default value
///     or were configured to use a <see cref="VistaDBValueGenerationStrategy" />.
/// </summary>
public class VistaDBValueGenerationConvention : RelationalValueGenerationConvention
{
    /// <summary>
    ///     Creates a new instance of <see cref="VistaDBValueGenerationConvention" />.
    /// </summary>
    /// <param name="dependencies">Parameter object containing dependencies for this convention.</param>
    /// <param name="relationalDependencies"> Parameter object containing relational dependencies for this convention.</param>
    public VistaDBValueGenerationConvention(
        ProviderConventionSetBuilderDependencies dependencies,
        RelationalConventionSetBuilderDependencies relationalDependencies)
        : base(dependencies, relationalDependencies)
    {
    }

    /// <summary>
    ///     Called after an annotation is changed on a property.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property.</param>
    /// <param name="name">The annotation name.</param>
    /// <param name="annotation">The new annotation.</param>
    /// <param name="oldAnnotation">The old annotation.</param>
    /// <param name="context">Additional information associated with convention execution.</param>
    public override void ProcessPropertyAnnotationChanged(
        IConventionPropertyBuilder propertyBuilder,
        string name,
        IConventionAnnotation? annotation,
        IConventionAnnotation? oldAnnotation,
        IConventionContext<IConventionAnnotation> context)
    {
        if (name == VistaDBAnnotationNames.ValueGenerationStrategy)
        {
            propertyBuilder.ValueGenerated(GetValueGenerated(propertyBuilder.Metadata));
            return;
        }

        base.ProcessPropertyAnnotationChanged(propertyBuilder, name, annotation, oldAnnotation, context);
    }

    // VistaDB: no analog — VistaDB has no temporal (system-versioned) tables, so the SqlServer override
    // for ProcessEntityTypeAnnotationChanged that reacted to TemporalPeriodStart/EndPropertyName annotation
    // changes is omitted. The base RelationalValueGenerationConvention behaviour is sufficient.
    // Original SqlServer logic preserved below for future revival when VistaDB adds support.
    /*
    public override void ProcessEntityTypeAnnotationChanged(
        IConventionEntityTypeBuilder entityTypeBuilder,
        string name,
        IConventionAnnotation? annotation,
        IConventionAnnotation? oldAnnotation,
        IConventionContext<IConventionAnnotation> context)
    {
        if (name is SqlServerAnnotationNames.TemporalPeriodStartPropertyName or SqlServerAnnotationNames.TemporalPeriodEndPropertyName
            && annotation?.Value is string propertyName)
        {
            var periodProperty = entityTypeBuilder.Metadata.FindProperty(propertyName);
            periodProperty?.Builder.ValueGenerated(GetValueGenerated(periodProperty));

            if (oldAnnotation?.Value is string oldPropertyName)
            {
                var oldPeriodProperty = entityTypeBuilder.Metadata.FindProperty(oldPropertyName);
                oldPeriodProperty?.Builder.ValueGenerated(GetValueGenerated(oldPeriodProperty));
            }
        }

        base.ProcessEntityTypeAnnotationChanged(entityTypeBuilder, name, annotation, oldAnnotation, context);
    }
    */

    /// <summary>
    ///     Returns the store value generation strategy to set for the given property.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>The store value generation strategy to set for the given property.</returns>
    protected override ValueGenerated? GetValueGenerated(IConventionProperty property)
    {
        var table = property.GetMappedStoreObjects(StoreObjectType.Table).FirstOrDefault();
        return table.Name != null
            ? GetValueGeneratedInternal(property, table)
            : property.DeclaringType.IsMappedToJson()
#pragma warning disable EF1001 // Internal EF Core API usage.
            && property.IsOrdinalKeyProperty()
#pragma warning restore EF1001 // Internal EF Core API usage.
            && (property.DeclaringType as IReadOnlyEntityType)?.FindOwnership()!.IsUnique == false
                ? ValueGenerated.OnAddOrUpdate
                : property.GetMappedStoreObjects(StoreObjectType.InsertStoredProcedure).Any()
                    ? GetValueGenerated((IReadOnlyProperty)property)
                    : null;
    }

    /// <inheritdoc/>
    protected override bool MappingStrategyAllowsValueGeneration(IConventionProperty property, string? mappingStrategy)
        => true;

    /// <summary>
    ///     Returns the store value generation strategy to set for the given property.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="storeObject">The identifier of the store object.</param>
    /// <returns>The store value generation strategy to set for the given property.</returns>
    public static new ValueGenerated? GetValueGenerated(IReadOnlyProperty property, in StoreObjectIdentifier storeObject)
        => RelationalValueGenerationConvention.GetValueGenerated(property, storeObject)
            ?? (GetVistaDBValueGenerationStrategy(property) != VistaDBValueGenerationStrategy.None
                ? ValueGenerated.OnAdd
                : null);

    private static ValueGenerated? GetValueGeneratedInternal(IReadOnlyProperty property, in StoreObjectIdentifier storeObject)
        => RelationalValueGenerationConvention.GetValueGenerated(property, storeObject)
            ?? (GetVistaDBValueGenerationStrategy(property) != VistaDBValueGenerationStrategy.None
                ? ValueGenerated.OnAdd
                : null);

    private static VistaDBValueGenerationStrategy GetVistaDBValueGenerationStrategy(IReadOnlyProperty property)
    {
        var annotation = property.FindAnnotation(VistaDBAnnotationNames.ValueGenerationStrategy);
        return annotation?.Value is VistaDBValueGenerationStrategy strategy
            ? strategy
            : VistaDBValueGenerationStrategy.None;
    }
}
