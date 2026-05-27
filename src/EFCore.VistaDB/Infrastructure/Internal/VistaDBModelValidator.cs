// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDB.Infrastructure.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBModelValidator : RelationalModelValidator
{
    // Annotation names for SqlServer features that are not in VistaDB's own annotation table.
    // We check for these literally so that a model carried over from SqlServer fails fast with a
    // clear error rather than producing invalid VistaDB DDL.
    private const string SqlServerMemoryOptimizedAnnotation = "SqlServer:MemoryOptimized";
    private const string SqlServerIsTemporalAnnotation = "SqlServer:IsTemporal";

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBModelValidator(
        ModelValidatorDependencies dependencies,
        RelationalModelValidatorDependencies relationalDependencies)
        : base(dependencies, relationalDependencies)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override void Validate(IModel model, IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
    {
        ValidateNoSchemas(model);
        ValidateNoSequences(model);
        ValidateNoComputedColumns(model);
        ValidateNoTemporalTables(model);
        ValidateNoMemoryOptimizedTables(model);

        base.Validate(model, logger);

        // VistaDB: no analog — SqlServer additionally validates decimal columns, vector columns,
        // byte-identity mapping, and temporal tables here. VistaDB rejects temporal tables outright
        // (see ValidateNoTemporalTables above), has no vector type, and the decimal/byte-identity
        // warnings rely on SqlServer-specific extensions.
        // Original SqlServer logic preserved below for future revival when VistaDB adds support.
        /*
        ValidateIndexIncludeProperties(model, logger);
        ValidateDecimalColumns(model, logger);
        ValidateVectorColumns(model, logger);
        ValidateByteIdentityMapping(model, logger);
        ValidateTemporalTables(model, logger);
        */
    }

    /// <summary>
    ///     Throws if any entity type is mapped to a non-default schema. VistaDB has no schemas.
    /// </summary>
    /// <param name="model">The model.</param>
    protected virtual void ValidateNoSchemas(IModel model)
    {
        foreach (var entityType in model.GetEntityTypes())
        {
            var schema = entityType.GetSchema();
            if (!string.IsNullOrEmpty(schema))
            {
                throw new InvalidOperationException(
                    VistaDBStrings.SchemasNotSupported(entityType.DisplayName(), schema));
            }
        }
    }

    /// <summary>
    ///     Throws if the model declares any sequences. VistaDB has no CREATE SEQUENCE.
    /// </summary>
    /// <param name="model">The model.</param>
    protected virtual void ValidateNoSequences(IModel model)
    {
        if (model.GetSequences().Any())
        {
            throw new InvalidOperationException(VistaDBStrings.SequencesNotSupported);
        }
    }

    /// <summary>
    ///     Throws if any property is configured with <c>HasComputedColumnSql</c>. VistaDB has no computed columns.
    /// </summary>
    /// <param name="model">The model.</param>
    protected virtual void ValidateNoComputedColumns(IModel model)
    {
        foreach (var entityType in model.GetEntityTypes())
        {
            foreach (var property in entityType.GetDeclaredProperties())
            {
                if (!string.IsNullOrEmpty(property.GetComputedColumnSql()))
                {
                    throw new InvalidOperationException(
                        VistaDBStrings.ComputedColumnsNotSupported(property.Name, entityType.DisplayName()));
                }
            }
        }
    }

    /// <summary>
    ///     Throws if any entity type is annotated as temporal (SqlServer:IsTemporal). VistaDB has no system-versioned tables.
    /// </summary>
    /// <param name="model">The model.</param>
    protected virtual void ValidateNoTemporalTables(IModel model)
    {
        foreach (var entityType in model.GetEntityTypes())
        {
            // VistaDB: no analog — SqlServer exposes entityType.IsTemporal() as a typed accessor.
            // We check the raw annotation so the validator does not depend on the SqlServer assembly.
            // Original SqlServer logic preserved below for future revival.
            /*
            if (entityType.IsTemporal())
            {
                throw new InvalidOperationException(
                    VistaDBStrings.TemporalTablesNotSupported(entityType.DisplayName()));
            }
            */
            if (entityType.FindAnnotation(SqlServerIsTemporalAnnotation)?.Value is true)
            {
                throw new InvalidOperationException(
                    VistaDBStrings.TemporalTablesNotSupported(entityType.DisplayName()));
            }
        }
    }

    /// <summary>
    ///     Throws if any entity type is annotated as memory-optimized (SqlServer:MemoryOptimized).
    ///     VistaDB has no memory-optimized tables.
    /// </summary>
    /// <param name="model">The model.</param>
    protected virtual void ValidateNoMemoryOptimizedTables(IModel model)
    {
        foreach (var entityType in model.GetEntityTypes())
        {
            // VistaDB: no analog — SqlServer exposes entityType.IsMemoryOptimized() as a typed accessor.
            // We check the raw annotation so the validator does not depend on the SqlServer assembly.
            // Original SqlServer logic preserved below for future revival.
            /*
            if (entityType.IsMemoryOptimized())
            {
                throw new InvalidOperationException(VistaDBStrings.MemoryOptimizedTablesNotSupported);
            }
            */
            if (entityType.FindAnnotation(SqlServerMemoryOptimizedAnnotation)?.Value is true)
            {
                throw new InvalidOperationException(VistaDBStrings.MemoryOptimizedTablesNotSupported);
            }
        }
    }
}
