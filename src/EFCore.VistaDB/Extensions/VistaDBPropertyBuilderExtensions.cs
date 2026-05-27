// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Metadata.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     VistaDB specific extension methods for <see cref="PropertyBuilder" />.
/// </summary>
public static class VistaDBPropertyBuilderExtensions
{
    /// <summary>
    ///     Configures the key property to use the VistaDB IDENTITY feature to generate values for new entities.
    ///     This method sets the property to be <see cref="ValueGenerated.OnAdd" />.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="seed">The value that is used for the very first row loaded into the table.</param>
    /// <param name="increment">The incremental value that is added to the identity value of the previous row that was loaded.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static PropertyBuilder UseIdentityColumn(
        this PropertyBuilder propertyBuilder,
        long seed = 1,
        int increment = 1)
    {
        var property = propertyBuilder.Metadata;
        property.SetOrRemoveAnnotation(VistaDBAnnotationNames.ValueGenerationStrategy, VistaDBValueGenerationStrategy.IdentityColumn);
        property.SetOrRemoveAnnotation(VistaDBAnnotationNames.IdentitySeed, seed);
        property.SetOrRemoveAnnotation(VistaDBAnnotationNames.IdentityIncrement, increment);
        return propertyBuilder;
    }

    /// <summary>
    ///     Configures the key property to use the VistaDB IDENTITY feature to generate values for new entities.
    ///     This method sets the property to be <see cref="ValueGenerated.OnAdd" />.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="seed">The value that is used for the very first row loaded into the table.</param>
    /// <param name="increment">The incremental value that is added to the identity value of the previous row that was loaded.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static PropertyBuilder UseIdentityColumn(
        this PropertyBuilder propertyBuilder,
        int seed,
        int increment = 1)
        => propertyBuilder.UseIdentityColumn((long)seed, increment);

    /// <summary>
    ///     Configures the key property to use the VistaDB IDENTITY feature to generate values for new entities.
    ///     This method sets the property to be <see cref="ValueGenerated.OnAdd" />.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property being configured.</typeparam>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="seed">The value that is used for the very first row loaded into the table.</param>
    /// <param name="increment">The incremental value that is added to the identity value of the previous row that was loaded.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static PropertyBuilder<TProperty> UseIdentityColumn<TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder,
        long seed = 1,
        int increment = 1)
        => (PropertyBuilder<TProperty>)UseIdentityColumn((PropertyBuilder)propertyBuilder, seed, increment);

    /// <summary>
    ///     Configures the key property to use the VistaDB IDENTITY feature to generate values for new entities.
    ///     This method sets the property to be <see cref="ValueGenerated.OnAdd" />.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property being configured.</typeparam>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="seed">The value that is used for the very first row loaded into the table.</param>
    /// <param name="increment">The incremental value that is added to the identity value of the previous row that was loaded.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static PropertyBuilder<TProperty> UseIdentityColumn<TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder,
        int seed,
        int increment = 1)
        => (PropertyBuilder<TProperty>)UseIdentityColumn((PropertyBuilder)propertyBuilder, (long)seed, increment);

    /// <summary>
    ///     Not supported by VistaDB — VistaDB has no database sequences.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="name">The name of the sequence.</param>
    /// <param name="schema">The schema of the sequence.</param>
    /// <returns>This method always throws <see cref="NotSupportedException" />.</returns>
    /// <exception cref="NotSupportedException">Always thrown — VistaDB does not support sequences.</exception>
    public static PropertyBuilder UseHiLo(
        this PropertyBuilder propertyBuilder,
        string? name = null,
        string? schema = null)
    {
        // VistaDB: no analog — VistaDB has no CREATE SEQUENCE, so the HiLo pattern cannot be used.
        // Original SqlServer logic preserved below for future revival when VistaDB adds support.
        /*
        Check.NullButNotEmpty(name);
        Check.NullButNotEmpty(schema);

        var property = propertyBuilder.Metadata;

        name ??= SqlServerModelExtensions.DefaultHiLoSequenceName;

        var model = property.DeclaringType.Model;

        if (model.FindSequence(name, schema) == null)
        {
            model.AddSequence(name, schema).IncrementBy = 10;
        }

        property.SetValueGenerationStrategy(SqlServerValueGenerationStrategy.SequenceHiLo);
        property.SetHiLoSequenceName(name);
        property.SetHiLoSequenceSchema(schema);
        property.SetIdentitySeed(null);
        property.SetIdentityIncrement(null);

        return propertyBuilder;
        */
        throw new NotSupportedException(VistaDBStrings.SequencesNotSupported);
    }

    /// <summary>
    ///     Not supported by VistaDB — VistaDB has no database sequences.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property being configured.</typeparam>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="name">The name of the sequence.</param>
    /// <param name="schema">The schema of the sequence.</param>
    /// <returns>This method always throws <see cref="NotSupportedException" />.</returns>
    /// <exception cref="NotSupportedException">Always thrown — VistaDB does not support sequences.</exception>
    public static PropertyBuilder<TProperty> UseHiLo<TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder,
        string? name = null,
        string? schema = null)
        => (PropertyBuilder<TProperty>)UseHiLo((PropertyBuilder)propertyBuilder, name, schema);

    /// <summary>
    ///     Not supported by VistaDB — VistaDB has no database sequences.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="name">The name of the sequence.</param>
    /// <param name="schema">The schema of the sequence.</param>
    /// <returns>This method always throws <see cref="NotSupportedException" />.</returns>
    /// <exception cref="NotSupportedException">Always thrown — VistaDB does not support sequences.</exception>
    public static PropertyBuilder UseSequence(
        this PropertyBuilder propertyBuilder,
        string? name = null,
        string? schema = null)
        => throw new NotSupportedException(VistaDBStrings.SequencesNotSupported);

    /// <summary>
    ///     Not supported by VistaDB — VistaDB has no database sequences.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property being configured.</typeparam>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="name">The name of the sequence.</param>
    /// <param name="schema">The schema of the sequence.</param>
    /// <returns>This method always throws <see cref="NotSupportedException" />.</returns>
    /// <exception cref="NotSupportedException">Always thrown — VistaDB does not support sequences.</exception>
    public static PropertyBuilder<TProperty> UseSequence<TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder,
        string? name = null,
        string? schema = null)
        => throw new NotSupportedException(VistaDBStrings.SequencesNotSupported);

    /// <summary>
    ///     Sets the <see cref="VistaDBValueGenerationStrategy" /> for the property.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="valueGenerationStrategy">The strategy to use.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns>
    ///     The same builder instance if the configuration was applied; <see langword="null" /> otherwise.
    /// </returns>
    public static IConventionPropertyBuilder? HasValueGenerationStrategy(
        this IConventionPropertyBuilder propertyBuilder,
        VistaDBValueGenerationStrategy? valueGenerationStrategy,
        bool fromDataAnnotation = false)
    {
        if (propertyBuilder.CanSetAnnotation(
                VistaDBAnnotationNames.ValueGenerationStrategy, valueGenerationStrategy, fromDataAnnotation))
        {
            propertyBuilder.Metadata.SetValueGenerationStrategy(valueGenerationStrategy, fromDataAnnotation);
            return propertyBuilder;
        }

        return null;
    }

    /// <summary>
    ///     Returns a value indicating whether the given <see cref="VistaDBValueGenerationStrategy" /> can be set
    ///     for the property.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="valueGenerationStrategy">The strategy to set.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns><see langword="true" /> if the given strategy can be set.</returns>
    public static bool CanSetValueGenerationStrategy(
        this IConventionPropertyBuilder propertyBuilder,
        VistaDBValueGenerationStrategy? valueGenerationStrategy,
        bool fromDataAnnotation = false)
        => propertyBuilder.CanSetAnnotation(
            VistaDBAnnotationNames.ValueGenerationStrategy, valueGenerationStrategy, fromDataAnnotation);
}
