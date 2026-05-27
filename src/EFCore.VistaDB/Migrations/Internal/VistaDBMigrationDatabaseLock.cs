// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Migrations.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
/// <remarks>
///     <para>
///         VistaDB: no analog — VistaDB is a file-based engine with no <c>sp_getapplock</c>/
///         <c>sp_releaseapplock</c> concept. The migration pipeline only needs
///         <see cref="IMigrationsDatabaseLock" /> as an <see cref="IDisposable" /> handle, so this
///         implementation is an inert no-op that satisfies the contract. Single-process file ownership is
///         enforced by VistaDB itself when the database file is opened.
///     </para>
///     <para>
///         Original SqlServer body is preserved verbatim in a marker block in the source for future revival
///         if VistaDB ever exposes a lock primitive.
///     </para>
/// </remarks>
public class VistaDBMigrationDatabaseLock : IMigrationsDatabaseLock
{
    // VistaDB: no analog — see <remarks> on the class. Original SqlServer body preserved below.
    /*
    public class SqlServerMigrationDatabaseLock(
        IRelationalCommand releaseLockCommand,
        RelationalCommandParameterObject relationalCommandParameters,
        IHistoryRepository historyRepository,
        CancellationToken cancellationToken = default)
        : IMigrationsDatabaseLock
    {
        public virtual IHistoryRepository HistoryRepository
            => historyRepository;

        public void Dispose()
            => releaseLockCommand.ExecuteScalar(relationalCommandParameters);

        public async ValueTask DisposeAsync()
            => await releaseLockCommand.ExecuteScalarAsync(relationalCommandParameters, cancellationToken).ConfigureAwait(false);
    }
    */

    private readonly IHistoryRepository _historyRepository;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBMigrationDatabaseLock(IHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual IHistoryRepository HistoryRepository
        => _historyRepository;

    /// <inheritdoc />
    public void Dispose()
    {
        // No-op: see remarks.
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;
}
