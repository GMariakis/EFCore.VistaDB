// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBTransaction : RelationalTransaction
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBTransaction(
        IRelationalConnection connection,
        DbTransaction transaction,
        Guid transactionId,
        IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> logger,
        bool transactionOwned,
        ISqlGenerationHelper sqlGenerationHelper)
        : base(connection, transaction, transactionId, logger, transactionOwned, sqlGenerationHelper)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    ///     VistaDB does not support savepoints in any form. Both <c>SAVE TRANSACTION &lt;name&gt;</c>
    ///     (SQL Server dialect) and the SQL-standard <c>SAVEPOINT &lt;name&gt;</c> are rejected by the
    ///     engine (errors 617 / 607 — verified by <c>SavepointSupportProbeTest</c>). When EF Core sees
    ///     <see cref="SupportsSavepoints" /> return <see langword="false" /> it skips emitting
    ///     <c>Create/Rollback/ReleaseSavepoint</c> calls during nested SaveChanges and silently
    ///     degrades to whole-transaction rollback semantics on failure. That matches what users would
    ///     expect from a file-based engine without savepoint support.
    /// </remarks>
    public override bool SupportsSavepoints
        => false;

    // VistaDB does not support savepoints at all, so neither the create/rollback nor the release paths
    // ever fire — but we keep these no-op overrides defensive in case EF Core ever calls them through
    // a path that bypasses the SupportsSavepoints gate.

    /// <inheritdoc />
    public override void ReleaseSavepoint(string name)
    {
    }

    /// <inheritdoc />
    public override Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
