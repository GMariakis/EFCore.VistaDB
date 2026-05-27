// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBParameterBasedSqlProcessor(
    RelationalParameterBasedSqlProcessorDependencies dependencies,
    RelationalParameterBasedSqlProcessorParameters parameters)
    : RelationalParameterBasedSqlProcessor(dependencies, parameters)
{
    // VistaDB: no analog — SqlServer runs two additional T-SQL passes: a zero-limit converter
    // (rewrites TOP(0) into a no-row predicate) and a search-condition converter (lifts boolean
    // projections into CASE WHEN). Both are useful for VistaDB in theory, but the relational base
    // produces SQL that VistaDB already accepts for the minimum-viable scenarios; ports can be
    // added if integration tests reveal regressions. Skipping the SqlServerSqlNullabilityProcessor
    // override for the same reason — the base implementation is sufficient.
    // Original SqlServer logic preserved below for future revival.
    /*
        private readonly ISqlServerSingletonOptions _sqlServerSingletonOptions;

        public override Expression Process(Expression queryExpression, ParametersCacheDecorator parametersDecorator)
        {
            var afterZeroLimitConversion = new SqlServerZeroLimitConverter(Dependencies.SqlExpressionFactory)
                .Process(queryExpression, parametersDecorator);

            var afterBaseProcessing = base.Process(afterZeroLimitConversion, parametersDecorator);

            var afterSearchConditionConversion = new SearchConditionConverter(Dependencies.SqlExpressionFactory)
                .Visit(afterBaseProcessing);

            return afterSearchConditionConversion;
        }

        protected override Expression ProcessSqlNullability(Expression selectExpression, ParametersCacheDecorator Decorator)
            => new SqlServerSqlNullabilityProcessor(Dependencies, Parameters, _sqlServerSingletonOptions)
                .Process(selectExpression, Decorator);
    */
}
