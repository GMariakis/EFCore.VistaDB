// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBQueryTranslationPostprocessor : RelationalQueryTranslationPostprocessor
{
    // VistaDB: no analog — SqlServer composes three extra postprocessors on top of the relational
    // base: a JSON postprocessor (lowers JSON_VALUE/OPENJSON), an aggregate-over-subquery lifter
    // (rewrites SUM(SELECT ...) as JOIN/OUTER APPLY), and a SqlServerSqlTreePruner. VistaDB has no
    // JSON support, supports neither OUTER APPLY nor subqueries inside aggregates (so we surface the
    // base "could not translate" error rather than rewriting), and uses the default SqlTreePruner.
    // Original SqlServer state preserved below for future revival when VistaDB grows support.
    /*
        private readonly SqlServerJsonPostprocessor _jsonPostprocessor;
        private readonly SqlServerAggregateOverSubqueryPostprocessor _aggregatePostprocessor;
        private readonly SqlServerSqlTreePruner _pruner = new();
    */

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBQueryTranslationPostprocessor(
        QueryTranslationPostprocessorDependencies dependencies,
        RelationalQueryTranslationPostprocessorDependencies relationalDependencies,
        VistaDBQueryCompilationContext queryCompilationContext)
        : base(dependencies, relationalDependencies, queryCompilationContext)
    {
    }

    // VistaDB: no analog — SqlServer's Process override runs the JSON and aggregate-over-subquery
    // postprocessors after the base pipeline. VistaDB needs neither.
    // Original SqlServer logic preserved below for future revival.
    /*
        public override Expression Process(Expression query)
        {
            var query1 = base.Process(query);
            var query2 = _jsonPostprocessor.Process(query1);
            var query3 = _aggregatePostprocessor.Visit(query2);
            return query3;
        }

        protected override Expression ProcessTypeMappings(Expression expression)
            => new SqlServerTypeMappingPostprocessor(Dependencies, RelationalDependencies, RelationalQueryCompilationContext).Process(
                expression);

        protected override Expression Prune(Expression query)
            => _pruner.Prune(query);
    */
}
