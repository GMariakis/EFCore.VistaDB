// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Metadata.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     VistaDB-specific extension methods for <see cref="IReadOnlyModel" />.
/// </summary>
/// <remarks>
///     Mirrors the SqlServer pattern at
///     <c>src/EFCore.SqlServer/Extensions/SqlServerModelExtensions.cs</c> — only the methods
///     required by <see cref="VistaDB.Metadata.Conventions.VistaDBValueGenerationStrategyConvention" />
///     are ported here. Sequence-related helpers (HiLo, sequence name/schema) are intentionally
///     omitted because VistaDB has no <c>CREATE SEQUENCE</c>; revive them when VistaDB adds support.
/// </remarks>
public static class VistaDBModelExtensions
{
    /// <summary>
    ///     Returns the <see cref="VistaDBValueGenerationStrategy" /> to use for properties of keys in the model,
    ///     unless the property has a strategy explicitly set.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>The default <see cref="VistaDBValueGenerationStrategy" />.</returns>
    public static VistaDBValueGenerationStrategy? GetValueGenerationStrategy(this IReadOnlyModel model)
        => (VistaDBValueGenerationStrategy?)model[VistaDBAnnotationNames.ValueGenerationStrategy];

    /// <summary>
    ///     Sets the <see cref="VistaDBValueGenerationStrategy" /> to use for properties of keys in the model,
    ///     unless the property has a strategy explicitly set.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="value">The value to set.</param>
    public static void SetValueGenerationStrategy(this IMutableModel model, VistaDBValueGenerationStrategy? value)
        => model.SetOrRemoveAnnotation(VistaDBAnnotationNames.ValueGenerationStrategy, value);

    /// <summary>
    ///     Sets the <see cref="VistaDBValueGenerationStrategy" /> to use for properties of keys in the model,
    ///     unless the property has a strategy explicitly set.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns>The configured value.</returns>
    public static VistaDBValueGenerationStrategy? SetValueGenerationStrategy(
        this IConventionModel model,
        VistaDBValueGenerationStrategy? value,
        bool fromDataAnnotation = false)
        => (VistaDBValueGenerationStrategy?)model.SetOrRemoveAnnotation(
                VistaDBAnnotationNames.ValueGenerationStrategy, value, fromDataAnnotation)
            ?.Value;

    /// <summary>
    ///     Returns the <see cref="ConfigurationSource" /> for the default <see cref="VistaDBValueGenerationStrategy" />.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>The <see cref="ConfigurationSource" /> for the default <see cref="VistaDBValueGenerationStrategy" />.</returns>
    public static ConfigurationSource? GetValueGenerationStrategyConfigurationSource(this IConventionModel model)
        => model.FindAnnotation(VistaDBAnnotationNames.ValueGenerationStrategy)?.GetConfigurationSource();
}
