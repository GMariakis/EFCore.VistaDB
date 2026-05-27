// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBNavigationExpansionExtensibilityHelper : NavigationExpansionExtensibilityHelper
{
    // VistaDB: no analog — SqlServer's overrides exist solely to support temporal-table query roots
    // (TemporalAsOf/Between/etc.), validating that navigation expansion across temporal/non-temporal
    // boundaries is rejected and that set operations across mismatched temporal sources fail. VistaDB
    // has no temporal-table feature, so the base implementation is sufficient.
    // Original SqlServer logic preserved below for future revival when VistaDB grows temporal-table support.
    /*
        public override EntityQueryRootExpression CreateQueryRoot(IEntityType entityType, EntityQueryRootExpression? source)
        {
            if (source is TemporalAsOfQueryRootExpression asOf)
            {
                return source.QueryProvider != null
                    ? new TemporalAsOfQueryRootExpression(source.QueryProvider, entityType, asOf.PointInTime)
                    : new TemporalAsOfQueryRootExpression(entityType, asOf.PointInTime);
            }

            return base.CreateQueryRoot(entityType, source);
        }

        public override void ValidateQueryRootCreation(IEntityType entityType, EntityQueryRootExpression? source)
        {
            // ... rejects temporal/non-temporal navigation crossings and non-AsOf navigation expansion.
        }

        public override bool AreQueryRootsCompatible(EntityQueryRootExpression? first, EntityQueryRootExpression? second)
        {
            // ... validates set-operation compatibility between two temporal query roots.
        }
    */

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBNavigationExpansionExtensibilityHelper(NavigationExpansionExtensibilityHelperDependencies dependencies)
        : base(dependencies)
    {
    }
}
