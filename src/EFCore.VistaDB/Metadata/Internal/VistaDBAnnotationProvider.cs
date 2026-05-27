// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDB.Metadata.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBAnnotationProvider : RelationalAnnotationProvider
{
    /// <summary>
    ///     Initializes a new instance of this class.
    /// </summary>
    /// <param name="dependencies">Parameter object containing dependencies for this service.</param>
    public VistaDBAnnotationProvider(RelationalAnnotationProviderDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override IEnumerable<IAnnotation> For(IRelationalModel model, bool designTime)
    {
        if (!designTime)
        {
            yield break;
        }

        // VistaDB: no analog — VistaDB has no Azure SQL edition options (MaxDatabaseSize, ServiceTier,
        // PerformanceLevel) and no Memory-Optimized tables.
        // Original SqlServer logic preserved below for future revival when VistaDB adds support.
        /*
        var maxSize = model.Model.GetDatabaseMaxSize();
        var serviceTier = model.Model.GetServiceTierSql();
        var performanceLevel = model.Model.GetPerformanceLevelSql();
        if (maxSize != null
            || serviceTier != null
            || performanceLevel != null)
        {
            var options = new StringBuilder();

            if (maxSize != null)
            {
                options.Append("MAXSIZE = ");
                options.Append(maxSize);
                options.Append(", ");
            }

            if (serviceTier != null)
            {
                options.Append("EDITION = ");
                options.Append(serviceTier);
                options.Append(", ");
            }

            if (performanceLevel != null)
            {
                options.Append("SERVICE_OBJECTIVE = ");
                options.Append(performanceLevel);
                options.Append(", ");
            }

            options.Remove(options.Length - 2, 2);

            yield return new Annotation(SqlServerAnnotationNames.EditionOptions, options.ToString());
        }

        if (model.Tables.Any(t => !t.IsExcludedFromMigrations && (t[SqlServerAnnotationNames.MemoryOptimized] as bool? == true)))
        {
            yield return new Annotation(SqlServerAnnotationNames.MemoryOptimized, true);
        }
        */
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override IEnumerable<IAnnotation> For(ITable table, bool designTime)
    {
        if (!designTime)
        {
            yield break;
        }

        // VistaDB: no analog — VistaDB has no Memory-Optimized tables and no Temporal (system-versioned) tables.
        // Original SqlServer logic preserved below for future revival when VistaDB adds support.
        /*
        var entityType = (IEntityType)table.EntityTypeMappings.First().TypeBase;

        // Model validation ensures that these facets are the same on all mapped entity types
        if (entityType.IsMemoryOptimized())
        {
            yield return new Annotation(SqlServerAnnotationNames.MemoryOptimized, true);
        }

        if (entityType.IsTemporal())
        {
            yield return new Annotation(SqlServerAnnotationNames.IsTemporal, true);
            yield return new Annotation(SqlServerAnnotationNames.TemporalHistoryTableName, entityType.GetHistoryTableName());
            yield return new Annotation(SqlServerAnnotationNames.TemporalHistoryTableSchema, entityType.GetHistoryTableSchema());

            var storeObjectIdentifier = StoreObjectIdentifier.Table(table.Name, table.Schema);
            var periodStartPropertyName = entityType.GetPeriodStartPropertyName();
            if (periodStartPropertyName != null)
            {
                var periodStartProperty = entityType.FindProperty(periodStartPropertyName);
                var periodStartColumnName = periodStartProperty != null
                    ? periodStartProperty.GetColumnName(storeObjectIdentifier)
                    : periodStartPropertyName;

                yield return new Annotation(SqlServerAnnotationNames.TemporalPeriodStartColumnName, periodStartColumnName);
            }

            var periodEndPropertyName = entityType.GetPeriodEndPropertyName();
            if (periodEndPropertyName != null)
            {
                var periodEndProperty = entityType.FindProperty(periodEndPropertyName);
                var periodEndColumnName = periodEndProperty != null
                    ? periodEndProperty.GetColumnName(storeObjectIdentifier)
                    : periodEndPropertyName;

                yield return new Annotation(SqlServerAnnotationNames.TemporalPeriodEndColumnName, periodEndColumnName);
            }
        }
        */
        yield break;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override IEnumerable<IAnnotation> For(IUniqueConstraint constraint, bool designTime)
    {
        if (!designTime)
        {
            yield break;
        }

        // VistaDB: no analog — VistaDB has no clustered/non-clustered configurability for keys and no FillFactor.
        // Original SqlServer logic preserved below for future revival when VistaDB adds support.
        /*
        var key = constraint.MappedKeys.First();
        var table = constraint.Table;

        if (key.IsClustered(StoreObjectIdentifier.Table(table.Name, table.Schema)) is { } isClustered)
        {
            yield return new Annotation(SqlServerAnnotationNames.Clustered, isClustered);
        }

        if (key.GetFillFactor() is { } fillFactor)
        {
            yield return new Annotation(SqlServerAnnotationNames.FillFactor, fillFactor);
        }
        */
        yield break;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override IEnumerable<IAnnotation> For(ITableIndex index, bool designTime)
    {
        if (!designTime)
        {
            yield break;
        }

        // VistaDB: no analog — VistaDB has no clustered indexes, no Include columns, no online index creation,
        // no FillFactor, no SortInTempDb, no DataCompression.
        // Original SqlServer logic preserved below for future revival when VistaDB adds support.
        /*
        var modelIndex = index.MappedIndexes.First();
        var table = StoreObjectIdentifier.Table(index.Table.Name, index.Table.Schema);
        if (modelIndex.IsClustered(table) is { } isClustered)
        {
            yield return new Annotation(SqlServerAnnotationNames.Clustered, isClustered);
        }

        if (modelIndex.GetIncludeProperties(table) is { } includeProperties)
        {
            var includeColumns = includeProperties
                .Select(p => modelIndex.DeclaringEntityType.FindProperty(p)!
                    .GetColumnName(StoreObjectIdentifier.Table(table.Name, table.Schema)))
                .ToArray();

            yield return new Annotation(
                SqlServerAnnotationNames.Include,
                includeColumns);
        }

        if (modelIndex.IsCreatedOnline(table) is { } isOnline)
        {
            yield return new Annotation(SqlServerAnnotationNames.CreatedOnline, isOnline);
        }

        if (modelIndex.GetFillFactor(table) is { } fillFactor)
        {
            yield return new Annotation(SqlServerAnnotationNames.FillFactor, fillFactor);
        }

        if (modelIndex.GetSortInTempDb(table) is { } sortInTempDb)
        {
            yield return new Annotation(SqlServerAnnotationNames.SortInTempDb, sortInTempDb);
        }

        if (modelIndex.GetDataCompression(table) is { } dataCompressionType)
        {
            yield return new Annotation(SqlServerAnnotationNames.DataCompression, dataCompressionType);
        }
        */
        yield break;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override IEnumerable<IAnnotation> For(IColumn column, bool designTime)
    {
        if (!designTime)
        {
            yield break;
        }

        var table = StoreObjectIdentifier.Table(column.Table.Name, column.Table.Schema);
        var identityProperty = column.PropertyMappings
            .Select(m => m.Property)
            .FirstOrDefault(p =>
            {
                var strategyAnnotation = p.FindAnnotation(VistaDBAnnotationNames.ValueGenerationStrategy);
                return strategyAnnotation?.Value is VistaDBValueGenerationStrategy strategy
                    && strategy == VistaDBValueGenerationStrategy.IdentityColumn;
            });
        if (identityProperty != null)
        {
            var seed = identityProperty.FindAnnotation(VistaDBAnnotationNames.IdentitySeed)?.Value as long?;
            var increment = identityProperty.FindAnnotation(VistaDBAnnotationNames.IdentityIncrement)?.Value as int?;

            yield return new Annotation(
                VistaDBAnnotationNames.Identity,
                string.Format(CultureInfo.InvariantCulture, "{0}, {1}", seed ?? 1, increment ?? 1));
        }

        // VistaDB: no analog — VistaDB has no Sparse columns, no Temporal period columns, and no
        // named default-constraint support.
        // Original SqlServer logic preserved below for future revival when VistaDB adds support.
        /*
        if (column is not JsonColumn)
        {
            if (column.PropertyMappings.FirstOrDefault()?.Property.IsSparse() is { } isSparse)
            {
                yield return new Annotation(SqlServerAnnotationNames.Sparse, isSparse);
            }

            var mappedProperty = column.PropertyMappings.FirstOrDefault()?.Property;
            if (mappedProperty != null)
            {
                if (mappedProperty.GetDefaultConstraintName(table) is { } defaultConstraintName)
                {
                    yield return new Annotation(RelationalAnnotationNames.DefaultConstraintName, defaultConstraintName);
                }
                else if (mappedProperty.DeclaringType.Model.AreNamedDefaultConstraintsUsed()
                         && (mappedProperty.FindAnnotation(RelationalAnnotationNames.DefaultValue) != null
                             || mappedProperty.FindAnnotation(RelationalAnnotationNames.DefaultValueSql) != null))
                {
                    yield return new Annotation(
                        RelationalAnnotationNames.DefaultConstraintName,
                        mappedProperty.GetDefaultDefaultConstraintName(table));
                }
            }
        }

        var entityType = (IEntityType)column.Table.EntityTypeMappings.First().TypeBase;
        if (entityType.IsTemporal())
        {
            // ... temporal period column annotation handling ...
        }
        */
    }
}
