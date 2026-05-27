// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBAggregateMethodCallTranslatorProvider : RelationalAggregateMethodCallTranslatorProvider
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBAggregateMethodCallTranslatorProvider(RelationalAggregateMethodCallTranslatorProviderDependencies dependencies)
        : base(dependencies)
    {
        // VistaDB: no analog — SqlServer registers SqlServerLongCountMethodTranslator (uses COUNT_BIG),
        // SqlServerStatisticsAggregateMethodTranslator (STDEV/STDEVP/VAR/VARP and friends) and
        // SqlServerStringAggregateMethodTranslator (STRING_AGG WITHIN GROUP). VistaDB does not provide
        // COUNT_BIG, the STDEV/VAR family, or STRING_AGG; the relational base supplies the standard
        // SUM/AVG/MIN/MAX/COUNT translators which are what VistaDB supports.
        // Original SqlServer registrations preserved below for future revival.
        /*
            var sqlExpressionFactory = dependencies.SqlExpressionFactory;
            var typeMappingSource = dependencies.RelationalTypeMappingSource;

            AddTranslators(
            [
                new SqlServerLongCountMethodTranslator(sqlExpressionFactory),
                new SqlServerStatisticsAggregateMethodTranslator(sqlExpressionFactory, typeMappingSource),
                new SqlServerStringAggregateMethodTranslator(sqlExpressionFactory, typeMappingSource)
            ]);
        */
    }
}
