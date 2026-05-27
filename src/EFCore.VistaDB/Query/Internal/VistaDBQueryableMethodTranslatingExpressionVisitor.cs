// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBQueryableMethodTranslatingExpressionVisitor : RelationalQueryableMethodTranslatingExpressionVisitor
{
    // VistaDB: no analog — SqlServer's override is ~775 lines and handles temporal-table query roots
    // (TemporalAsOf/Between/etc.), structural-JSON / OPENJSON collection expansion, multi-setter
    // ExecuteUpdate via MERGE OUTPUT, and CTE-introducing split-query plans. None of those features
    // exist on VistaDB. The relational base implementation produces translation failures for these
    // patterns, surfacing a clear "could not translate" error — which is the desired behaviour.
    // Original SqlServer state preserved below for future revival.
    /*
        private static readonly bool UseOldBehavior37016 =
            AppContext.TryGetSwitch("Microsoft.EntityFrameworkCore.Issue37016", out var enabled) && enabled;

        private readonly SqlServerQueryCompilationContext _queryCompilationContext;
        private readonly IRelationalTypeMappingSource _typeMappingSource;
        private readonly ISqlExpressionFactory _sqlExpressionFactory;
        private readonly ISqlServerSingletonOptions _sqlServerSingletonOptions;

        private HashSet<ColumnExpression>? _columnsWithMultipleSetters;
    */

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBQueryableMethodTranslatingExpressionVisitor(
        QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
        RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies,
        VistaDBQueryCompilationContext queryCompilationContext)
        : base(dependencies, relationalDependencies, queryCompilationContext)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected VistaDBQueryableMethodTranslatingExpressionVisitor(
        VistaDBQueryableMethodTranslatingExpressionVisitor parentVisitor)
        : base(parentVisitor)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override QueryableMethodTranslatingExpressionVisitor CreateSubqueryVisitor()
        => new VistaDBQueryableMethodTranslatingExpressionVisitor(this);
}
