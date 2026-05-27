// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Metadata.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     VistaDB-specific extension methods for <see cref="IReadOnlyProperty" />.
/// </summary>
/// <remarks>
///     Mirrors the SqlServer pattern at
///     <c>src/EFCore.SqlServer/Extensions/SqlServerPropertyExtensions.cs</c> — only the
///     <c>GetValueGenerationStrategy</c> resolvers required by
///     <see cref="VistaDB.Metadata.Conventions.VistaDBValueGenerationStrategyConvention" /> are ported,
///     plus the seed / increment accessors used by the migrations SQL generator. HiLo / Sequence
///     branches from the SqlServer version are dropped because VistaDB has no <c>CREATE SEQUENCE</c>.
/// </remarks>
public static class VistaDBPropertyExtensions
{
    /// <summary>
    ///     Returns the <see cref="VistaDBValueGenerationStrategy" /> to use for the property.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>The strategy, or <see cref="VistaDBValueGenerationStrategy.None" /> if none was set.</returns>
    public static VistaDBValueGenerationStrategy GetValueGenerationStrategy(this IReadOnlyProperty property)
    {
        var annotation = property.FindAnnotation(VistaDBAnnotationNames.ValueGenerationStrategy);
        if (annotation != null)
        {
            return (VistaDBValueGenerationStrategy?)annotation.Value ?? VistaDBValueGenerationStrategy.None;
        }

        if (property.ValueGenerated != ValueGenerated.OnAdd
            || property.IsForeignKey()
            || property.TryGetDefaultValue(out _)
            || property.GetDefaultValueSql() != null
            || property.GetComputedColumnSql() != null)
        {
            return VistaDBValueGenerationStrategy.None;
        }

        return GetDefaultValueGenerationStrategy(property);
    }

    /// <summary>
    ///     Returns the <see cref="VistaDBValueGenerationStrategy" /> to use for the property, taking
    ///     account of the per-table-store-object overrides.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="storeObject">The identifier of the store object.</param>
    /// <returns>The strategy, or <see cref="VistaDBValueGenerationStrategy.None" /> if none was set.</returns>
    public static VistaDBValueGenerationStrategy GetValueGenerationStrategy(
        this IReadOnlyProperty property,
        in StoreObjectIdentifier storeObject)
        => GetValueGenerationStrategy(property, storeObject, null);

    /// <summary>
    ///     Returns the <see cref="VistaDBValueGenerationStrategy" /> to use for the property, taking
    ///     account of the per-table-store-object overrides and provider type mappings.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="storeObject">The identifier of the store object.</param>
    /// <param name="typeMappingSource">The relational type mapping source, used to determine the provider CLR type.</param>
    /// <returns>The strategy, or <see cref="VistaDBValueGenerationStrategy.None" /> if none was set.</returns>
    internal static VistaDBValueGenerationStrategy GetValueGenerationStrategy(
        this IReadOnlyProperty property,
        in StoreObjectIdentifier storeObject,
        ITypeMappingSource? typeMappingSource)
    {
        var @override = property.FindOverrides(storeObject)?.FindAnnotation(VistaDBAnnotationNames.ValueGenerationStrategy);
        if (@override != null)
        {
            return (VistaDBValueGenerationStrategy?)@override.Value ?? VistaDBValueGenerationStrategy.None;
        }

        var annotation = property.FindAnnotation(VistaDBAnnotationNames.ValueGenerationStrategy);
        if (annotation?.Value != null
            && StoreObjectIdentifier.Create(property.DeclaringType, storeObject.StoreObjectType) == storeObject)
        {
            return (VistaDBValueGenerationStrategy)annotation.Value;
        }

        // Copy 'in' parameter to a local so lambdas (Any) can capture it.
        var capturedStoreObject = storeObject;

        var sharedTableRootProperty = property.FindSharedStoreObjectRootProperty(storeObject);
        if (sharedTableRootProperty != null)
        {
            return sharedTableRootProperty.GetValueGenerationStrategy(storeObject, typeMappingSource)
                == VistaDBValueGenerationStrategy.IdentityColumn
                && storeObject.StoreObjectType == StoreObjectType.Table
                && !property.GetContainingForeignKeys().Any(
                    fk => !fk.IsBaseLinking()
                        || (StoreObjectIdentifier.Create(fk.PrincipalEntityType, StoreObjectType.Table)
                                is StoreObjectIdentifier principal
                            && fk.GetConstraintName(capturedStoreObject, principal) != null))
                    ? VistaDBValueGenerationStrategy.IdentityColumn
                    : VistaDBValueGenerationStrategy.None;
        }

        if (property.ValueGenerated != ValueGenerated.OnAdd
            || storeObject.StoreObjectType != StoreObjectType.Table
            || property.TryGetDefaultValue(storeObject, out _)
            || property.GetDefaultValueSql(storeObject) != null
            || property.GetComputedColumnSql(storeObject) != null
            || property.GetContainingForeignKeys().Any(
                fk => !fk.IsBaseLinking()
                    || (StoreObjectIdentifier.Create(fk.PrincipalEntityType, StoreObjectType.Table)
                            is StoreObjectIdentifier principal
                        && fk.GetConstraintName(capturedStoreObject, principal) != null)))
        {
            return VistaDBValueGenerationStrategy.None;
        }

        return GetDefaultValueGenerationStrategy(property, storeObject, typeMappingSource);
    }

    /// <summary>
    ///     Sets the <see cref="VistaDBValueGenerationStrategy" /> to use for the property.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="value">The strategy to set.</param>
    public static void SetValueGenerationStrategy(
        this IMutableProperty property,
        VistaDBValueGenerationStrategy? value)
        => property.SetOrRemoveAnnotation(VistaDBAnnotationNames.ValueGenerationStrategy, value);

    /// <summary>
    ///     Sets the <see cref="VistaDBValueGenerationStrategy" /> to use for the property.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="value">The strategy to set.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns>The configured value.</returns>
    public static VistaDBValueGenerationStrategy? SetValueGenerationStrategy(
        this IConventionProperty property,
        VistaDBValueGenerationStrategy? value,
        bool fromDataAnnotation = false)
        => (VistaDBValueGenerationStrategy?)property.SetOrRemoveAnnotation(
                VistaDBAnnotationNames.ValueGenerationStrategy, value, fromDataAnnotation)
            ?.Value;

    /// <summary>
    ///     Returns the <see cref="ConfigurationSource" /> for the property's <see cref="VistaDBValueGenerationStrategy" />.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>The <see cref="ConfigurationSource" />, or <see langword="null" /> if not set.</returns>
    public static ConfigurationSource? GetValueGenerationStrategyConfigurationSource(this IConventionProperty property)
        => property.FindAnnotation(VistaDBAnnotationNames.ValueGenerationStrategy)?.GetConfigurationSource();

    private static VistaDBValueGenerationStrategy GetDefaultValueGenerationStrategy(IReadOnlyProperty property)
    {
        var modelStrategy = property.DeclaringType.Model.GetValueGenerationStrategy();

        return modelStrategy == VistaDBValueGenerationStrategy.IdentityColumn
            && IsCompatibleWithValueGeneration(property)
                ? VistaDBValueGenerationStrategy.IdentityColumn
                : VistaDBValueGenerationStrategy.None;
    }

    private static VistaDBValueGenerationStrategy GetDefaultValueGenerationStrategy(
        IReadOnlyProperty property,
        in StoreObjectIdentifier storeObject,
        ITypeMappingSource? typeMappingSource)
    {
        var modelStrategy = property.DeclaringType.Model.GetValueGenerationStrategy();

        return modelStrategy == VistaDBValueGenerationStrategy.IdentityColumn
            && IsCompatibleWithValueGeneration(property, storeObject, typeMappingSource)
                ? VistaDBValueGenerationStrategy.IdentityColumn
                : VistaDBValueGenerationStrategy.None;
    }

    /// <summary>
    ///     Returns the identity seed for this property.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>The identity seed, or 1 if not configured.</returns>
    public static long? GetIdentitySeed(this IReadOnlyProperty property)
        => (long?)property[VistaDBAnnotationNames.IdentitySeed];

    /// <summary>
    ///     Returns the identity increment for this property.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>The identity increment, or 1 if not configured.</returns>
    public static int? GetIdentityIncrement(this IReadOnlyProperty property)
        => (int?)property[VistaDBAnnotationNames.IdentityIncrement];

    private static bool IsCompatibleWithValueGeneration(IReadOnlyProperty property)
    {
        var valueConverter = property.GetValueConverter()
            ?? property.FindTypeMapping()?.Converter;

        var type = (valueConverter?.ProviderClrType ?? property.ClrType).UnwrapNullableType();
        return type.IsInteger() || type == typeof(decimal);
    }

    private static bool IsCompatibleWithValueGeneration(
        IReadOnlyProperty property,
        in StoreObjectIdentifier storeObject,
        ITypeMappingSource? typeMappingSource)
    {
        var valueConverter = property.GetValueConverter()
            ?? (property.FindRelationalTypeMapping(storeObject)
                ?? typeMappingSource?.FindMapping((IProperty)property))?.Converter;

        var type = (valueConverter?.ProviderClrType ?? property.ClrType).UnwrapNullableType();
        return type.IsInteger() || type == typeof(decimal);
    }
}
