// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Update.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
/// <remarks>
///     The service lifetime is <see cref="ServiceLifetime.Singleton" />. This means a single instance
///     is used by many <see cref="DbContext" /> instances. The implementation must be thread-safe.
///     This service cannot depend on services registered as <see cref="ServiceLifetime.Scoped" />.
/// </remarks>
public interface IVistaDBUpdateSqlGenerator : IUpdateSqlGenerator
{
    // VistaDB: no analog — SqlServer exposes a bulk-insert hook here that emits MERGE ... OUTPUT batches.
    // VistaDB supports neither MERGE nor OUTPUT, so there is no batched form distinct from the per-row INSERT.
    // Original SqlServer surface preserved below for future revival when VistaDB adds support.
    /*
        ResultSetMapping AppendBulkInsertOperation(
            StringBuilder commandStringBuilder,
            IReadOnlyList<IReadOnlyModificationCommand> modificationCommands,
            int commandPosition,
            out bool requiresTransaction);

        ResultSetMapping AppendBulkInsertOperation(
            StringBuilder commandStringBuilder,
            IReadOnlyList<IReadOnlyModificationCommand> modificationCommands,
            int commandPosition)
            => AppendBulkInsertOperation(commandStringBuilder, modificationCommands, commandPosition, out _);
    */
}
