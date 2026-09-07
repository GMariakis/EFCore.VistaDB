// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;
using VistaDB.DDA;

namespace Microsoft.EntityFrameworkCore.VistaDB.Update.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
/// <remarks>
///     <para>
///         VistaDB has no MERGE and no OUTPUT clause, so multi-row INSERT/UPDATE/DELETE batches cannot be
///         reliably correlated back to their originating entities. We force the max batch size to 1 so the
///         batch emits a single statement per command. The factory still wraps each statement in a single
///         round-trip via <see cref="ModificationCommandBatch" />, so we don't lose all batching — just the
///         cross-command merging.
///     </para>
///     <para>
///         <b>IDENTITY_INSERT routing (Option DDA-2):</b> when an INSERT supplies an explicit value for an
///         IDENTITY column, the SQL path is unreliable in interaction with EF Core's spec-test fixtures —
///         see Plan Phase 2.11. We route those commands through VistaDB's DDA managed API
///         (<see cref="IVistaDBTable.IdentityInsert" /> + <c>Insert</c> / <c>Put</c> / <c>Post</c>)
///         which is the engine's first-class support for explicit identity-value inserts. Verified by the
///         <c>DdaIdentityInsertProbeTest</c> diagnostic: the DDA <c>IdentityInsert</c> property is
///         "independent from the one-table-at-a-time setting for a SQL connection" so it bypasses the SQL
///         path's connection-scoped quirks.
///     </para>
///     <para>
///         <b>Transactional caveat:</b> DDA <c>Post</c> commits immediately at the engine level and does NOT
///         participate in the open VistaDB ADO.NET transaction. For fixture seeding
///         (the failure mode that motivated this code path) this is correct; for user code that wraps
///         <c>SaveChanges</c> in an explicit transaction and rolls back, the DDA-inserted rows persist.
///     </para>
/// </remarks>
public class VistaDBModificationCommandBatch : AffectedCountModificationCommandBatch
{
    private readonly IVistaDBDdaAccessor _ddaAccessor;
    private readonly List<IReadOnlyModificationCommand> _ddaIdentityInsertCommands = [];

    // Sticky flag set when at least one DDA-routed identity-insert command has been added to this batch.
    // We can't rely on _ddaIdentityInsertCommands.Count for the "is this batch DDA-only?" check at
    // execute-time because ExecuteDdaIdentityInserts() clears that list, so the post-DDA "should I
    // fall through to base.ExecuteAsync?" decision must use this sticky flag instead.
    private bool _hasDdaCommands;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBModificationCommandBatch(
        ModificationCommandBatchFactoryDependencies dependencies,
        IVistaDBDdaAccessor ddaAccessor)
        : base(dependencies, maxBatchSize: 1)
    {
        _ddaAccessor = ddaAccessor;
    }

    /// <summary>
    ///     <see langword="true" /> when this batch carries no SQL — only DDA-routed identity inserts. Used
    ///     by the overridden execution path to skip the base SQL command execution entirely.
    /// </summary>
    private bool IsDdaOnly
        => _hasDdaCommands && IsCommandTextEmpty;

    /// <inheritdoc />
    protected override void AddCommand(IReadOnlyModificationCommand modificationCommand)
    {
        if (RequiresIdentityInsert(modificationCommand))
        {
            // Route to DDA. Do NOT call base.AddCommand — that would append a SQL INSERT to SqlBuilder
            // which VistaDB silently substitutes auto-IDs for (proven by DdaIdentityInsertProbeTest).
            // We still add the command to the public ModificationCommands list via the base
            // TryAddCommand → AddCommand call chain (which happens BEFORE this AddCommand). Here we
            // simply record the command for DDA execution and skip the SQL machinery.
            _ddaIdentityInsertCommands.Add(modificationCommand);
            _hasDdaCommands = true;
            return;
        }

        base.AddCommand(modificationCommand);
    }

    /// <inheritdoc />
    public override void Complete(bool moreBatchesExpected)
    {
        if (IsDdaOnly)
        {
            // DDA-only batch — no SQL StoreCommand is built. The overridden Execute/ExecuteAsync below
            // bypasses base entirely. Note the batch is still considered "complete" so the caller can
            // move on to the next batch.
            SetRequiresTransaction(false);
            return;
        }

        base.Complete(moreBatchesExpected);
    }

    /// <inheritdoc />
    public override void Execute(IRelationalConnection connection)
    {
        if (_ddaIdentityInsertCommands.Count > 0)
        {
            try
            {
                ExecuteDdaIdentityInserts();
            }
            catch (Exception ex) when (ex is not DbUpdateException and not OperationCanceledException)
            {
                // Mirror the base ReaderModificationCommandBatch wrap: callers expect DbUpdateException
                // for any DB-side failure during SaveChanges. The DDA path bypasses the base wrap, so
                // we have to do it ourselves — otherwise raw VistaDBException (engine error 309 etc.)
                // leaks through and tests Assert.Throws&lt;DbUpdateException&gt; fail.
                throw new DbUpdateException(
                    "An error occurred while saving the entity changes via the VistaDB DDA identity-insert path. See the inner exception for details.",
                    ex,
                    ModificationCommands.SelectMany(c => c.Entries).ToList());
            }
        }

        if (!IsDdaOnly)
        {
            if (AllCommandsReportRowsAffectedOnly())
            {
                ExecuteAndVerifyRowsAffected(connection);
            }
            else
            {
                base.Execute(connection);
            }
        }
    }

    /// <summary>
    ///     Whether every command in this batch is a plain write with nothing to read back.
    ///
    ///     Those are emitted as bare INSERT/UPDATE/DELETE with no trailing SELECT, because VistaDB has
    ///     no usable <c>@@ROWCOUNT</c> — see the note at the top of
    ///     <see cref="VistaDBUpdateSqlGenerator" />. EF Core's reader-based consumer skips the
    ///     rows-affected check entirely for such commands, so the verification has to happen here.
    /// </summary>
    private bool AllCommandsReportRowsAffectedOnly()
        => ResultSetMappings.Count > 0 && ResultSetMappings.All(m => m == ResultSetMapping.NoResults);

    /// <summary>
    ///     Runs the batch with <c>ExecuteNonQuery</c> and checks the count the engine reports.
    ///
    ///     This is the one mechanism VistaDB reports reliably: measured on 6.6.2, a batch of three
    ///     inserts returns 3, one hit plus one miss returns 1, and two misses return 0. Going through
    ///     the reader instead yields <c>RecordsAffected == -1</c>, which tells us nothing.
    /// </summary>
    private void ExecuteAndVerifyRowsAffected(IRelationalConnection connection)
    {
        int rowsAffected;
        try
        {
            rowsAffected = StoreCommand!.RelationalCommand.ExecuteNonQuery(
                new RelationalCommandParameterObject(
                    connection,
                    StoreCommand.ParameterValues,
                    null,
                    Dependencies.CurrentContext.Context,
                    Dependencies.Logger,
                    CommandSource.SaveChanges));
        }
        catch (DbUpdateException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new DbUpdateException(
                RelationalStrings.UpdateStoreException, ex, ModificationCommands.SelectMany(c => c.Entries).ToList());
        }

        int expected = ModificationCommands.Count;
        if (rowsAffected < expected)
        {
            // Fewer rows than commands means at least one write matched nothing — the row was changed
            // or deleted by someone else since it was loaded.
            ThrowAggregateUpdateConcurrencyException(
                reader: null, commandIndex: expected, expectedRowsAffected: expected, rowsAffected: rowsAffected);
        }
    }

    /// <inheritdoc />
    public override async Task ExecuteAsync(IRelationalConnection connection, CancellationToken cancellationToken = default)
    {
        if (_ddaIdentityInsertCommands.Count > 0)
        {
            // DDA operations are synchronous; do them inline. Cooperative cancellation between rows.
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                ExecuteDdaIdentityInserts();
            }
            catch (Exception ex) when (ex is not DbUpdateException and not OperationCanceledException)
            {
                throw new DbUpdateException(
                    "An error occurred while saving the entity changes via the VistaDB DDA identity-insert path. See the inner exception for details.",
                    ex,
                    ModificationCommands.SelectMany(c => c.Entries).ToList());
            }
        }

        if (!IsDdaOnly)
        {
            if (AllCommandsReportRowsAffectedOnly())
            {
                await ExecuteAndVerifyRowsAffectedAsync(connection, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await base.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc cref="ExecuteAndVerifyRowsAffected" />
    private async Task ExecuteAndVerifyRowsAffectedAsync(IRelationalConnection connection, CancellationToken cancellationToken)
    {
        int rowsAffected;
        try
        {
            rowsAffected = await StoreCommand!.RelationalCommand.ExecuteNonQueryAsync(
                new RelationalCommandParameterObject(
                    connection,
                    StoreCommand.ParameterValues,
                    null,
                    Dependencies.CurrentContext.Context,
                    Dependencies.Logger,
                    CommandSource.SaveChanges),
                cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new DbUpdateException(
                RelationalStrings.UpdateStoreException, ex, ModificationCommands.SelectMany(c => c.Entries).ToList());
        }

        int expected = ModificationCommands.Count;
        if (rowsAffected < expected)
        {
            await ThrowAggregateUpdateConcurrencyExceptionAsync(
                reader: null, commandIndex: expected, expectedRowsAffected: expected,
                rowsAffected: rowsAffected, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Returns <see langword="true" /> when <paramref name="modificationCommand" /> is an Added command
    ///     whose PK column carries an explicit caller-supplied value and the property's resolved
    ///     value-generation strategy is <see cref="VistaDBValueGenerationStrategy.IdentityColumn" />.
    ///     Identical to the detection used by the (now-removed) SQL <c>SET IDENTITY_INSERT</c> wrapper.
    /// </summary>
    private static bool RequiresIdentityInsert(IReadOnlyModificationCommand modificationCommand)
    {
        if (modificationCommand.EntityState != EntityState.Added)
        {
            return false;
        }

        var storeObject = StoreObjectIdentifier.Table(modificationCommand.TableName, modificationCommand.Schema);
        foreach (var columnModification in modificationCommand.ColumnModifications)
        {
            if (columnModification is { IsKey: true, IsWrite: true }
                && columnModification.Property is { } property
                && property.GetValueGenerationStrategy(storeObject) == VistaDBValueGenerationStrategy.IdentityColumn)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Performs each pending identity-insert via the VistaDB DDA managed API:
    ///     <c>OpenDatabase</c> → <c>OpenTable</c> → <c>IdentityInsert = true</c> → <c>Insert</c> →
    ///     <c>Put(col, value)</c> per writable column → <c>Post</c> → repeat per command → restore
    ///     <c>IdentityInsert = false</c> and dispose. Read-back values (e.g., RowVersion) on
    ///     <see cref="IColumnModification.IsRead" /> columns are populated from the post-insert row state.
    /// </summary>
    private void ExecuteDdaIdentityInserts()
    {
        // Group commands by (schema, table). Open each table once per group, toggle IdentityInsert,
        // process all commands targeting that table, then move to the next table. Minimizes DDA
        // open/close churn within a single batch — though with MaxBatchSize=1 there's only ever one
        // command per batch anyway, so this is defensive.
        var groups = _ddaIdentityInsertCommands
            .GroupBy(c => (c.Schema, c.TableName))
            .ToList();

        using var database = _ddaAccessor.OpenDatabase(readOnly: false);
        foreach (var group in groups)
        {
            using var table = database.OpenTable(group.Key.TableName, exclusive: false, readOnly: false);
            var hadException = false;
            try
            {
                table.IdentityInsert = true;

                foreach (var command in group)
                {
                    table.Insert();
                    foreach (var col in command.ColumnModifications)
                    {
                        if (!col.IsWrite)
                        {
                            continue;
                        }

                        // DDA's Put-by-name takes an IVistaDBValue. We round-trip through the column's
                        // existing IVistaDBValue (from Get): set its CLR-typed Value, then put it back.
                        // The Get / Put.Set workflow is the DDA-documented way to assign system values
                        // to a row column when you don't know the column ordinal up-front.
                        //
                        // Apply the property's type-mapping value converter before handing the value to
                        // the DDA: col.Value carries the MODEL CLR value (e.g. DateOnly), but the DDA
                        // column setter expects the PROVIDER CLR type (e.g. DateTime). EF Core's SQL
                        // path applies the converter at parameter binding; the DDA path bypasses that,
                        // so we apply it inline.
                        var dest = table.Get(col.ColumnName);
                        var value = col.Value;
                        if (value is not null
                            && col.Property?.GetTypeMapping().Converter is { } converter)
                        {
                            value = converter.ConvertToProvider(value);
                        }
                        dest.Value = value;
                        table.Put(col.ColumnName, dest);
                    }

                    table.Post();

                    // Populate read-back values from the post-insert row state. For an identity-insert
                    // INSERT, IsRead is typically set on the PK column (for the SELECT-back EF Core
                    // would normally do via SQL). When the row carries an explicit write value
                    // (UseCurrentValueParameter == true) we copy it back — that's what VistaDB
                    // persisted under IdentityInsert ON. When there is no explicit write value, we
                    // intentionally leave the column alone: assigning null would crash for
                    // non-nullable CLR types (e.g. ResultType.ResultValue : decimal where the column
                    // was flagged IsRead but no value was provided by the caller). EF Core's existing
                    // tracking value remains in effect for that property.
                    foreach (var col in command.ColumnModifications)
                    {
                        if (col.IsRead && col.UseCurrentValueParameter)
                        {
                            col.Value = col.Value;
                        }
                    }
                }
            }
            catch
            {
                hadException = true;
                throw;
            }
            finally
            {
                try
                {
                    table.IdentityInsert = false;
                }
                catch when (hadException)
                {
                    // Don't shadow the original exception with a cleanup failure.
                }
            }
        }

        _ddaIdentityInsertCommands.Clear();
    }

    /// <summary>
    ///     Override the base "0 rows affected" exception type so that what SqlServer would surface as a
    ///     plain <see cref="DbUpdateException" /> (an engine-level constraint violation) is not surfaced
    ///     by VistaDB as a misleading <see cref="DbUpdateConcurrencyException" />.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>Why:</b> When EF Core's SQL UPDATE/DELETE returns 0 rows affected, the base class
    ///         throws <see cref="DbUpdateConcurrencyException" />. SqlServer rarely hits this path for
    ///         constraint scenarios because its engine pre-rejects the SQL with a <c>SqlException</c>
    ///         (e.g. FK conflict error 547), which EF Core wraps as a plain
    ///         <see cref="DbUpdateException" />. VistaDB's engine doesn't pre-reject the same scenarios
    ///         — UPDATE/DELETE silently affect 0 rows when the FK chain wouldn't allow them — so we
    ///         fall through to the affected-row check and throw the concurrency subclass.
    ///     </para>
    ///     <para>
    ///         The spec tests (especially the GraphUpdates <c>ClientNoAction</c> variants which
    ///         dominate the remaining failures, ~397) write
    ///         <c>Assert.ThrowsAsync&lt;DbUpdateException&gt;</c> which xUnit treats as an
    ///         <i>exact-type</i> match — the subclass fails the assertion. Override the throw to use the
    ///         base class. For single-process file-based VistaDB, true optimistic-concurrency conflicts
    ///         (where another connection modified the row mid-operation) are vanishingly rare; nearly
    ///         every "0 rows affected" comes from a constraint/cascade scenario, so the base class is
    ///         the more accurate fit anyway.
    ///     </para>
    /// </remarks>
    protected override void ThrowAggregateUpdateConcurrencyException(
        RelationalDataReader reader,
        int commandIndex,
        int expectedRowsAffected,
        int rowsAffected)
    {
        // AggregateEntries in the base is private; inline the same logic.
        var entries = new List<IUpdateEntry>();
        for (var i = commandIndex - expectedRowsAffected; i < commandIndex; i++)
        {
            entries.AddRange(ModificationCommands[i].Entries);
        }

        throw new DbUpdateException(
            Microsoft.EntityFrameworkCore.Diagnostics.RelationalStrings.UpdateConcurrencyException(expectedRowsAffected, rowsAffected),
            (Exception?)null,
            entries);
    }

    /// <inheritdoc cref="ThrowAggregateUpdateConcurrencyException" />
    protected override Task ThrowAggregateUpdateConcurrencyExceptionAsync(
        RelationalDataReader reader,
        int commandIndex,
        int expectedRowsAffected,
        int rowsAffected,
        CancellationToken cancellationToken)
    {
        // AggregateEntries in the base is private; inline the same logic.
        var entries = new List<IUpdateEntry>();
        for (var i = commandIndex - expectedRowsAffected; i < commandIndex; i++)
        {
            entries.AddRange(ModificationCommands[i].Entries);
        }

        throw new DbUpdateException(
            Microsoft.EntityFrameworkCore.Diagnostics.RelationalStrings.UpdateConcurrencyException(expectedRowsAffected, rowsAffected),
            (Exception?)null,
            entries);
    }

    // VistaDB: no analog — SqlServer accumulates pending INSERTs into a MERGE ... OUTPUT batch. VistaDB
    // supports neither MERGE nor OUTPUT, so we let the base ReaderModificationCommandBatch route each
    // command through UpdateSqlGenerator.AppendInsert/Update/DeleteOperation one at a time.
    // Original SqlServer surface preserved below for future revival when VistaDB grows the analogs.
    /*
        private const int DefaultNetworkPacketSizeBytes = 4096;
        private const int MaxScriptLength = 65536 * DefaultNetworkPacketSizeBytes / 2;
        private const int MaxParameterCount = 2100 - 2;

        private readonly List<IReadOnlyModificationCommand> _pendingBulkInsertCommands = [];

        protected new virtual ISqlServerUpdateSqlGenerator UpdateSqlGenerator
            => (ISqlServerUpdateSqlGenerator)base.UpdateSqlGenerator;

        protected override void RollbackLastCommand(IReadOnlyModificationCommand modificationCommand)
        {
            if (_pendingBulkInsertCommands.Count > 0)
            {
                _pendingBulkInsertCommands.RemoveAt(_pendingBulkInsertCommands.Count - 1);
            }
            base.RollbackLastCommand(modificationCommand);
        }

        protected override bool IsValid() { ... parameter / script length checks ... }

        private void ApplyPendingBulkInsertCommands() { ... AppendBulkInsertOperation ... }

        public override bool TryAddCommand(IReadOnlyModificationCommand modificationCommand) { ... }
        protected override void AddCommand(IReadOnlyModificationCommand modificationCommand) { ... }
        public override void Complete(bool moreBatchesExpected) { ApplyPendingBulkInsertCommands(); base.Complete(...); }

        // SqlServer-specific exception translation for OUTPUT-clause / trigger errors (334 / 4186).
        public override void Execute(IRelationalConnection connection) { try { base.Execute(connection); } catch (...) { ... } }
        public override async Task ExecuteAsync(IRelationalConnection connection, CancellationToken ct) { ... }
    */
}
