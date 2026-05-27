// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Transactions;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using IsolationLevel = System.Data.IsolationLevel;

namespace Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
/// <remarks>
///     VistaDB: no analog — replaces <see cref="MigrationCommandExecutor" /> in DI so that any
///     <see cref="VistaDBDdaMigrationCommand" /> in the migration stream is dispatched against the
///     <see cref="IVistaDBDdaAccessor" /> bound to the current <see cref="IVistaDBConnection" />,
///     while regular SQL <see cref="MigrationCommand" /> instances still flow through normal
///     <see cref="DbCommand.ExecuteNonQuery" /> execution.
/// </remarks>
public class VistaDBDdaMigrationCommandExecutor : IMigrationCommandExecutor
{
    private readonly IExecutionStrategy _executionStrategy;
    private readonly IVistaDBDdaAccessor _ddaAccessor;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBDdaMigrationCommandExecutor(IExecutionStrategy executionStrategy, IVistaDBDdaAccessor ddaAccessor)
    {
        _executionStrategy = executionStrategy;
        _ddaAccessor = ddaAccessor;
    }

    /// <inheritdoc />
    public virtual void ExecuteNonQuery(
        IEnumerable<MigrationCommand> migrationCommands,
        IRelationalConnection connection)
        => ExecuteNonQuery(migrationCommands.ToList(), connection, new MigrationExecutionState(), commitTransaction: true);

    /// <inheritdoc />
    public virtual int ExecuteNonQuery(
        IReadOnlyList<MigrationCommand> migrationCommands,
        IRelationalConnection connection,
        MigrationExecutionState executionState,
        bool commitTransaction,
        IsolationLevel? isolationLevel = null)
    {
        var inUserTransaction = connection.CurrentTransaction is not null && executionState.Transaction == null;
        if (inUserTransaction
            && (migrationCommands.Any(x => x.TransactionSuppressed) || _executionStrategy.RetriesOnFailure))
        {
            throw new NotSupportedException(RelationalStrings.TransactionSuppressedMigrationInUserTransaction);
        }

        using var transactionScope = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);

        return _executionStrategy.Execute(
            (migrationCommands, connection, inUserTransaction, executionState, commitTransaction, isolationLevel, accessor: _ddaAccessor),
            static (_, s) => Execute(
                s.migrationCommands,
                s.connection,
                s.executionState,
                beginTransaction: !s.inUserTransaction,
                commitTransaction: !s.inUserTransaction && s.commitTransaction,
                s.isolationLevel,
                s.accessor),
            verifySucceeded: null);
    }

    /// <inheritdoc />
    public virtual Task ExecuteNonQueryAsync(
        IEnumerable<MigrationCommand> migrationCommands,
        IRelationalConnection connection,
        CancellationToken cancellationToken = default)
        => ExecuteNonQueryAsync(
            migrationCommands.ToList(), connection, new MigrationExecutionState(), commitTransaction: true, IsolationLevel.Unspecified,
            cancellationToken);

    /// <inheritdoc />
    public virtual async Task<int> ExecuteNonQueryAsync(
        IReadOnlyList<MigrationCommand> migrationCommands,
        IRelationalConnection connection,
        MigrationExecutionState executionState,
        bool commitTransaction,
        IsolationLevel? isolationLevel = null,
        CancellationToken cancellationToken = default)
    {
        var inUserTransaction = connection.CurrentTransaction is not null && executionState.Transaction == null;
        if (inUserTransaction
            && (migrationCommands.Any(x => x.TransactionSuppressed) || _executionStrategy.RetriesOnFailure))
        {
            throw new NotSupportedException(RelationalStrings.TransactionSuppressedMigrationInUserTransaction);
        }

        using var transactionScope = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);

        return await _executionStrategy.ExecuteAsync(
            (migrationCommands, connection, inUserTransaction, executionState, commitTransaction, isolationLevel, accessor: _ddaAccessor),
            static (_, s, ct) => ExecuteAsync(
                s.migrationCommands,
                s.connection,
                s.executionState,
                beginTransaction: !s.inUserTransaction,
                commitTransaction: !s.inUserTransaction && s.commitTransaction,
                s.isolationLevel,
                s.accessor,
                ct),
            verifySucceeded: null,
            cancellationToken).ConfigureAwait(false);
    }

    private static int Execute(
        IReadOnlyList<MigrationCommand> migrationCommands,
        IRelationalConnection connection,
        MigrationExecutionState executionState,
        bool beginTransaction,
        bool commitTransaction,
        IsolationLevel? isolationLevel,
        IVistaDBDdaAccessor accessor)
    {
        var result = 0;
        var connectionOpened = connection.Open();

        try
        {
            for (var i = executionState.LastCommittedCommandIndex; i < migrationCommands.Count; i++)
            {
                var command = migrationCommands[i];
                if (executionState.Transaction == null
                    && !command.TransactionSuppressed
                    && beginTransaction)
                {
                    executionState.Transaction = isolationLevel == null
                        ? connection.BeginTransaction()
                        : connection.BeginTransaction(isolationLevel.Value);
                    if (executionState.DatabaseLock != null)
                    {
                        executionState.DatabaseLock = executionState.DatabaseLock.ReacquireIfNeeded(
                            connectionOpened, transactionRestarted: true);
                        connectionOpened = false;
                    }
                }

                if (executionState.Transaction != null
                    && command.TransactionSuppressed)
                {
                    executionState.Transaction.Commit();
                    executionState.Transaction.Dispose();
                    executionState.Transaction = null;
                    executionState.LastCommittedCommandIndex = i;
                    executionState.AnyOperationPerformed = true;

                    if (executionState.DatabaseLock != null)
                    {
                        executionState.DatabaseLock = executionState.DatabaseLock.ReacquireIfNeeded(
                            connectionOpened, transactionRestarted: null);
                        connectionOpened = false;
                    }
                }

                if (command is VistaDBDdaMigrationCommand ddaCommand)
                {
                    ddaCommand.ExecuteDda(accessor);
                    result = 0;
                }
                else
                {
                    result = command.ExecuteNonQuery(connection);
                }

                if (executionState.Transaction == null)
                {
                    executionState.LastCommittedCommandIndex = i + 1;
                    executionState.AnyOperationPerformed = true;
                }
            }

            if (commitTransaction
                && executionState.Transaction != null)
            {
                executionState.Transaction.Commit();
                executionState.Transaction.Dispose();
                executionState.Transaction = null;
            }
        }
        catch
        {
            executionState.Transaction?.Dispose();
            executionState.Transaction = null;
            connection.Close();
            throw;
        }

        connection.Close();
        return result;
    }

    private static async Task<int> ExecuteAsync(
        IReadOnlyList<MigrationCommand> migrationCommands,
        IRelationalConnection connection,
        MigrationExecutionState executionState,
        bool beginTransaction,
        bool commitTransaction,
        IsolationLevel? isolationLevel,
        IVistaDBDdaAccessor accessor,
        CancellationToken cancellationToken)
    {
        var result = 0;
        var connectionOpened = await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            for (var i = executionState.LastCommittedCommandIndex; i < migrationCommands.Count; i++)
            {
                var lockReacquired = false;
                var command = migrationCommands[i];
                if (executionState.Transaction == null
                    && !command.TransactionSuppressed
                    && beginTransaction)
                {
                    executionState.Transaction = await (isolationLevel == null
                            ? connection.BeginTransactionAsync(cancellationToken)
                            : connection.BeginTransactionAsync(isolationLevel.Value, cancellationToken))
                        .ConfigureAwait(false);

                    if (executionState.DatabaseLock != null)
                    {
                        executionState.DatabaseLock = await executionState.DatabaseLock.ReacquireIfNeededAsync(
                                connectionOpened, transactionRestarted: true, cancellationToken)
                            .ConfigureAwait(false);
                        lockReacquired = true;
                    }
                }

                if (executionState.Transaction != null
                    && command.TransactionSuppressed)
                {
                    await executionState.Transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    await executionState.Transaction.DisposeAsync().ConfigureAwait(false);
                    executionState.Transaction = null;
                    executionState.LastCommittedCommandIndex = i;
                    executionState.AnyOperationPerformed = true;

                    if (executionState.DatabaseLock != null
                        && !lockReacquired)
                    {
                        executionState.DatabaseLock = await executionState.DatabaseLock.ReacquireIfNeededAsync(
                                connectionOpened, transactionRestarted: null, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }

                if (command is VistaDBDdaMigrationCommand ddaCommand)
                {
                    await ddaCommand.ExecuteDdaAsync(accessor, cancellationToken).ConfigureAwait(false);
                    result = 0;
                }
                else
                {
                    result = await command.ExecuteNonQueryAsync(connection, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }

                if (executionState.Transaction == null)
                {
                    executionState.LastCommittedCommandIndex = i + 1;
                    executionState.AnyOperationPerformed = true;
                }
            }

            if (commitTransaction
                && executionState.Transaction != null)
            {
                await executionState.Transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                await executionState.Transaction.DisposeAsync().ConfigureAwait(false);
                executionState.Transaction = null;
            }
        }
        catch
        {
            if (executionState.Transaction != null)
            {
                await executionState.Transaction.DisposeAsync().ConfigureAwait(false);
                executionState.Transaction = null;
            }

            await connection.CloseAsync().ConfigureAwait(false);
            throw;
        }

        await connection.CloseAsync().ConfigureAwait(false);
        return result;
    }
}
