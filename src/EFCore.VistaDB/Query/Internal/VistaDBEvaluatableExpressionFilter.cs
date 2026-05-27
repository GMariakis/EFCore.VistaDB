// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBEvaluatableExpressionFilter : RelationalEvaluatableExpressionFilter
{
    // VistaDB: no analog — SqlServer marks calls into SqlServerDbFunctionsExtensions as non-evaluatable so
    // they reach the translator stack. VistaDB does not yet expose a VistaDBDbFunctionsExtensions surface;
    // when one is introduced, mirror this filter to block its calls from client-side evaluation.
    // Original SqlServer logic preserved below for future revival.
    /*
        public override bool IsEvaluatableExpression(Expression expression, IModel model)
        {
            if (expression is MethodCallExpression methodCallExpression
                && methodCallExpression.Method.DeclaringType == typeof(SqlServerDbFunctionsExtensions))
            {
                return false;
            }

            return base.IsEvaluatableExpression(expression, model);
        }
    */

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBEvaluatableExpressionFilter(
        EvaluatableExpressionFilterDependencies dependencies,
        RelationalEvaluatableExpressionFilterDependencies relationalDependencies)
        : base(dependencies, relationalDependencies)
    {
    }
}
