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
    /// <summary>
    ///     Runs the base relational processing, then converts boolean expressions between
    ///     search-condition and value form. VistaDB has no boolean type, so a bare <c>bit</c> column in a
    ///     <c>WHERE</c> is rejected with error 666; the converter turns it into an explicit comparison,
    ///     and in the other direction wraps predicates in <c>CASE WHEN</c> so they can be projected. It
    ///     runs last because EF's own optimisations would otherwise collapse the comparison away again.
    /// </summary>
    public override Expression Process(Expression queryExpression, ParametersCacheDecorator parametersDecorator)
        => new VistaDBSearchConditionConverter(Dependencies.SqlExpressionFactory)
            .Visit(base.Process(queryExpression, parametersDecorator));

    // VistaDB: no analog — SqlServer also runs a zero-limit converter (rewriting TOP(0) into a no-row
    // predicate) and overrides ProcessSqlNullability with a SqlServer-specific processor. Neither is
    // needed here; the relational base produces SQL VistaDB accepts.
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
