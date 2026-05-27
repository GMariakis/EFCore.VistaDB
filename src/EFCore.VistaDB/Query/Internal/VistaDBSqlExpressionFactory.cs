// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBSqlExpressionFactory : SqlExpressionFactory
{
    // VistaDB: no analog — SqlServer's ApplyTypeMapping override specialises AtTimeZoneExpression
    // and SqlServerAggregateFunctionExpression. VistaDB supports neither AT TIME ZONE nor custom
    // WITHIN GROUP aggregates, so the base implementation is sufficient.
    // Original SqlServer logic preserved below for future revival.
    /*
        private readonly IRelationalTypeMappingSource _typeMappingSource;

        [return: NotNullIfNotNull(nameof(sqlExpression))]
        public override SqlExpression? ApplyTypeMapping(SqlExpression? sqlExpression, RelationalTypeMapping? typeMapping)
            => sqlExpression switch
            {
                null or { TypeMapping: not null } => sqlExpression,

                AtTimeZoneExpression e => ApplyTypeMappingOnAtTimeZone(e, typeMapping),
                SqlServerAggregateFunctionExpression e => e.ApplyTypeMapping(typeMapping),

                _ => base.ApplyTypeMapping(sqlExpression, typeMapping)
            };

        private SqlExpression ApplyTypeMappingOnAtTimeZone(AtTimeZoneExpression atTimeZoneExpression, RelationalTypeMapping? typeMapping)
        {
            // ... applies precision-aware datetime2 mapping for AT TIME ZONE operands.
        }
    */

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBSqlExpressionFactory(
        SqlExpressionFactoryDependencies dependencies)
        : base(dependencies)
    {
    }
}
