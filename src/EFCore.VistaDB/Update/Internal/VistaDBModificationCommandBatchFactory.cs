// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDB.Update.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBModificationCommandBatchFactory : IModificationCommandBatchFactory
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    private readonly IVistaDBDdaAccessor _ddaAccessor;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBModificationCommandBatchFactory(
        ModificationCommandBatchFactoryDependencies dependencies,
        IVistaDBDdaAccessor ddaAccessor)
    {
        Dependencies = dependencies;
        _ddaAccessor = ddaAccessor;
    }

    // VistaDB: no analog — SqlServer reads a configurable MaxBatchSize from the options extension here
    // (clamped to 1..1000) and threads it into SqlServerModificationCommandBatch for use as the MERGE batch
    // size. VistaDB forces MaxBatchSize = 1 because we have no MERGE/OUTPUT path; the configured value is
    // ignored. Original SqlServer logic preserved below for future revival.
    /*
        private const int DefaultMaxBatchSize = 42;
        private const int MaxMaxBatchSize = 1000;
        private readonly int _maxBatchSize;

        public SqlServerModificationCommandBatchFactory(
            ModificationCommandBatchFactoryDependencies dependencies,
            IDbContextOptions options)
        {
            Dependencies = dependencies;
            _maxBatchSize = Math.Min(
                options.Extensions.OfType<SqlServerOptionsExtension>().FirstOrDefault()?.MaxBatchSize ?? DefaultMaxBatchSize,
                MaxMaxBatchSize);
            if (_maxBatchSize <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(RelationalOptionsExtension.MaxBatchSize), RelationalStrings.InvalidMaxBatchSize(_maxBatchSize));
            }
        }
    */

    /// <summary>
    ///     Relational provider-specific dependencies for this service.
    /// </summary>
    protected virtual ModificationCommandBatchFactoryDependencies Dependencies { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual ModificationCommandBatch Create()
        => new VistaDBModificationCommandBatch(Dependencies, _ddaAccessor);
}
