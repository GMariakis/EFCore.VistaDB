// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
/// <remarks>
///     VistaDB: no analog — SqlServer migration commands are always SQL text. VistaDB needs a hook to invoke
///     the managed DDA API for operations SQL cannot express (e.g. table-schema alterations that the VistaDB
///     T-SQL surface does not support). This subclass piggybacks on <see cref="MigrationCommand" /> so it can
///     flow through the existing migration pipeline; the executor (<see cref="VistaDBDdaMigrationCommandExecutor" />)
///     short-circuits SQL execution when it sees an instance of this type.
/// </remarks>
public class VistaDBDdaMigrationCommand : MigrationCommand
{
    private readonly Action<IVistaDBDdaAccessor>? _syncAction;
    private readonly Func<IVistaDBDdaAccessor, CancellationToken, Task>? _asyncAction;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    /// <param name="sentinelCommand">
    ///     A no-op <see cref="IRelationalCommand" /> carrying sentinel SQL (e.g. <c>-- VistaDB DDA marker</c>) so that the
    ///     base <see cref="MigrationCommand" /> contract is satisfied; this SQL is never executed by
    ///     <see cref="VistaDBDdaMigrationCommandExecutor" />.
    /// </param>
    /// <param name="context">The current <see cref="DbContext" /> or <see langword="null" /> if not known.</param>
    /// <param name="logger">The command logger.</param>
    /// <param name="syncAction">The synchronous DDA action to invoke.</param>
    /// <param name="asyncAction">
    ///     The asynchronous DDA action to invoke. If <see langword="null" />, the executor wraps
    ///     <paramref name="syncAction" /> in a completed task.
    /// </param>
    /// <param name="transactionSuppressed">Indicates whether or not transactions should be suppressed while executing the command.</param>
    public VistaDBDdaMigrationCommand(
        IRelationalCommand sentinelCommand,
        DbContext? context,
        IRelationalCommandDiagnosticsLogger logger,
        Action<IVistaDBDdaAccessor>? syncAction = null,
        Func<IVistaDBDdaAccessor, CancellationToken, Task>? asyncAction = null,
        bool transactionSuppressed = true)
        : base(sentinelCommand, context, logger, transactionSuppressed)
    {
        if (syncAction is null && asyncAction is null)
        {
            throw new ArgumentException("At least one of syncAction or asyncAction must be supplied.", nameof(syncAction));
        }

        _syncAction = syncAction;
        _asyncAction = asyncAction;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual void ExecuteDda(IVistaDBDdaAccessor accessor)
    {
        if (_syncAction is not null)
        {
            _syncAction(accessor);
        }
        else
        {
            // Bridge async-only into sync via GetAwaiter().GetResult().
            _asyncAction!(accessor, CancellationToken.None).GetAwaiter().GetResult();
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual Task ExecuteDdaAsync(IVistaDBDdaAccessor accessor, CancellationToken cancellationToken = default)
    {
        if (_asyncAction is not null)
        {
            return _asyncAction(accessor, cancellationToken);
        }

        _syncAction!(accessor);
        return Task.CompletedTask;
    }
}
