// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBCompiledQueryCacheKeyGenerator : RelationalCompiledQueryCacheKeyGenerator
{
    // VistaDB: no analog — SqlServer's GenerateCacheKey wraps the relational key with a MARS flag
    // (IsMultipleActiveResultSetsEnabled). VistaDB has no MARS, so the relational key is sufficient.
    // Original SqlServer logic preserved below for future revival.
    /*
        private readonly ISqlServerConnection _sqlServerConnection;

        public override object GenerateCacheKey(Expression query, bool async)
            => new SqlServerCompiledQueryCacheKey(
                GenerateCacheKeyCore(query, async),
                _sqlServerConnection.IsMultipleActiveResultSetsEnabled);
    */

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBCompiledQueryCacheKeyGenerator(
        CompiledQueryCacheKeyGeneratorDependencies dependencies,
        RelationalCompiledQueryCacheKeyGeneratorDependencies relationalDependencies)
        : base(dependencies, relationalDependencies)
    {
    }
}
