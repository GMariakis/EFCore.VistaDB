// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBQuerySqlGeneratorFactory : IQuerySqlGeneratorFactory
{
    private readonly IRelationalTypeMappingSource _typeMappingSource;

    // VistaDB: no analog — VistaDB has a single engine (file-based .vdb6) with no engine-type or
    // compatibility-level discriminator, so we drop the ISqlServerSingletonOptions dependency.
    // Original SqlServer logic preserved below for future revival when VistaDB adds variants.
    /*
        private readonly ISqlServerSingletonOptions _sqlServerSingletonOptions;
    */

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBQuerySqlGeneratorFactory(
        QuerySqlGeneratorDependencies dependencies,
        IRelationalTypeMappingSource typeMappingSource)
    {
        Dependencies = dependencies;
        _typeMappingSource = typeMappingSource;
    }

    /// <summary>
    ///     Relational provider-specific dependencies for this service.
    /// </summary>
    protected virtual QuerySqlGeneratorDependencies Dependencies { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual QuerySqlGenerator Create()
        => new VistaDBQuerySqlGenerator(Dependencies, _typeMappingSource);
}
