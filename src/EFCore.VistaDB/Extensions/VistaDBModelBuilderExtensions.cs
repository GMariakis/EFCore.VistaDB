// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Metadata.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     VistaDB-specific extension methods for <see cref="ModelBuilder" />.
/// </summary>
/// <remarks>
///     Mirrors the SqlServer pattern at
///     <c>src/EFCore.SqlServer/Extensions/SqlServerModelBuilderExtensions.cs</c> — only the
///     model-level <c>HasValueGenerationStrategy</c> overload required by
///     <see cref="VistaDB.Metadata.Conventions.VistaDBValueGenerationStrategyConvention" /> is ported.
///     HiLo / identity-seed / identity-increment side effects from the SqlServer version are dropped
///     because VistaDB doesn't support sequences and the seed/increment are property-level concerns.
/// </remarks>
public static class VistaDBModelBuilderExtensions
{
    /// <summary>
    ///     Configures the default value generation strategy for key properties marked as <see cref="ValueGenerated.OnAdd" />,
    ///     when targeting VistaDB.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="valueGenerationStrategy">The value generation strategy.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns>
    ///     The same builder instance if the configuration was applied; <see langword="null" /> otherwise.
    /// </returns>
    public static IConventionModelBuilder? HasValueGenerationStrategy(
        this IConventionModelBuilder modelBuilder,
        VistaDBValueGenerationStrategy? valueGenerationStrategy,
        bool fromDataAnnotation = false)
    {
        if (modelBuilder.CanSetValueGenerationStrategy(valueGenerationStrategy, fromDataAnnotation))
        {
            modelBuilder.Metadata.SetValueGenerationStrategy(valueGenerationStrategy, fromDataAnnotation);
            return modelBuilder;
        }

        return null;
    }

    /// <summary>
    ///     Returns a value indicating whether the given value can be set as the default value generation strategy.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="valueGenerationStrategy">The value generation strategy.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns><see langword="true" /> if the given value can be set as the default value generation strategy.</returns>
    public static bool CanSetValueGenerationStrategy(
        this IConventionModelBuilder modelBuilder,
        VistaDBValueGenerationStrategy? valueGenerationStrategy,
        bool fromDataAnnotation = false)
        => modelBuilder.CanSetAnnotation(
            VistaDBAnnotationNames.ValueGenerationStrategy, valueGenerationStrategy, fromDataAnnotation);
}
