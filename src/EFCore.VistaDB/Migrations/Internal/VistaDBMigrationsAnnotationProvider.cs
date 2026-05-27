// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Metadata.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDB.Migrations.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
/// <remarks>
///     <para>
///         VistaDB has no temporal-table, memory-optimized, sparse, vector, or sequence annotations to
///         forward to migration operations — only the identity-strategy annotations (which the
///         <see cref="VistaDBAnnotationProvider" /> already attaches in <c>For(IColumn, ...)</c>) survive.
///         Original SqlServer forwarding logic is preserved in a marker block in the source for future
///         revival when VistaDB adds support for any of these features.
///     </para>
/// </remarks>
public class VistaDBMigrationsAnnotationProvider : MigrationsAnnotationProvider
{
    // VistaDB: no analog — temporal-period/temporal-history/default-constraint-name annotations have no
    // VistaDB counterparts. The original SqlServer body is preserved verbatim below for future revival.
    /*
    public override IEnumerable<IAnnotation> ForRemove(IRelationalModel model)
        => model.GetAnnotations().Where(a => a.Name != SqlServerAnnotationNames.EditionOptions);

    public override IEnumerable<IAnnotation> ForRemove(ITable table)
        => table.GetAnnotations();

    public override IEnumerable<IAnnotation> ForRemove(IColumn column)
    {
        if (column.Table[SqlServerAnnotationNames.IsTemporal] as bool? == true)
        {
            if (column[SqlServerAnnotationNames.TemporalIsPeriodStartColumn] as bool? == true)
                yield return new Annotation(SqlServerAnnotationNames.TemporalIsPeriodStartColumn, true);
            if (column[SqlServerAnnotationNames.TemporalIsPeriodEndColumn] as bool? == true)
                yield return new Annotation(SqlServerAnnotationNames.TemporalIsPeriodEndColumn, true);
        }

        if (column[RelationalAnnotationNames.DefaultConstraintName] is string defaultConstraintName)
            yield return new Annotation(RelationalAnnotationNames.DefaultConstraintName, defaultConstraintName);
    }

    public override IEnumerable<IAnnotation> ForRename(ITable table)
    {
        if (table[SqlServerAnnotationNames.IsTemporal] as bool? == true)
        {
            yield return new Annotation(SqlServerAnnotationNames.IsTemporal, true);
            yield return new Annotation(SqlServerAnnotationNames.TemporalHistoryTableName, table[SqlServerAnnotationNames.TemporalHistoryTableName]);
            yield return new Annotation(SqlServerAnnotationNames.TemporalHistoryTableSchema, table[SqlServerAnnotationNames.TemporalHistoryTableSchema]);
            yield return new Annotation(SqlServerAnnotationNames.TemporalPeriodStartColumnName, table[SqlServerAnnotationNames.TemporalPeriodStartColumnName]);
            yield return new Annotation(SqlServerAnnotationNames.TemporalPeriodEndColumnName, table[SqlServerAnnotationNames.TemporalPeriodEndColumnName]);
        }
    }

    public override IEnumerable<IAnnotation> ForRename(IColumn column)
    {
        if (column[SqlServerAnnotationNames.TemporalIsPeriodStartColumn] as bool? == true)
            yield return new Annotation(SqlServerAnnotationNames.TemporalIsPeriodStartColumn, true);
        if (column[SqlServerAnnotationNames.TemporalIsPeriodEndColumn] as bool? == true)
            yield return new Annotation(SqlServerAnnotationNames.TemporalIsPeriodEndColumn, true);
    }
    */

    /// <summary>
    ///     Initializes a new instance of this class.
    /// </summary>
    /// <param name="dependencies">Parameter object containing dependencies for this service.</param>
#pragma warning disable EF1001 // Internal EF Core API usage.
    public VistaDBMigrationsAnnotationProvider(MigrationsAnnotationProviderDependencies dependencies)
#pragma warning restore EF1001 // Internal EF Core API usage.
        : base(dependencies)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<IAnnotation> ForRemove(IRelationalModel model)
        => model.GetAnnotations();

    /// <inheritdoc />
    public override IEnumerable<IAnnotation> ForRemove(ITable table)
        => table.GetAnnotations();

    /// <inheritdoc />
    public override IEnumerable<IAnnotation> ForRemove(IColumn column)
    {
        // Forward only the identity annotations (used by the SQL generator when emitting DROP COLUMN
        // followed by ADD COLUMN). VistaDB has no temporal or default-constraint-name annotations.
        if (column[VistaDBAnnotationNames.Identity] is not null)
        {
            yield return new Annotation(VistaDBAnnotationNames.Identity, column[VistaDBAnnotationNames.Identity]);
        }

        if (column[VistaDBAnnotationNames.IdentitySeed] is not null)
        {
            yield return new Annotation(VistaDBAnnotationNames.IdentitySeed, column[VistaDBAnnotationNames.IdentitySeed]);
        }

        if (column[VistaDBAnnotationNames.IdentityIncrement] is not null)
        {
            yield return new Annotation(
                VistaDBAnnotationNames.IdentityIncrement, column[VistaDBAnnotationNames.IdentityIncrement]);
        }
    }

    /// <inheritdoc />
    public override IEnumerable<IAnnotation> ForRename(ITable table)
        => [];

    /// <inheritdoc />
    public override IEnumerable<IAnnotation> ForRename(IColumn column)
        => [];
}
