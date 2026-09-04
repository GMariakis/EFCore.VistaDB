// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDB.Migrations.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
/// <remarks>
///     <para>
///         Mirrors <c>SqlServerHistoryRepository</c>. Key differences:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 VistaDB has no <c>OBJECT_ID()</c> / <c>sys.tables</c>, and it does not ship the
///                 SQL Server-style <c>INFORMATION_SCHEMA</c> views either — querying them fails with
///                 error 627 ("Invalid schema name. DBO must be used instead of: INFORMATION_SCHEMA").
///                 Existence is therefore probed through the DDA surface
///                 (<see cref="IVistaDBDdaAccessor" />), matching how
///                 <c>VistaDBDatabaseModelFactory</c> enumerates tables during scaffolding.
///             </description>
///         </item>
///         <item>
///             <description>
///                 VistaDB has no <c>sp_getapplock</c>/<c>sp_releaseapplock</c>.
///                 <see cref="AcquireDatabaseLock" /> returns an inert
///                 <see cref="VistaDBMigrationDatabaseLock" />; single-process ownership of the <c>.vdb6</c>
///                 file is enforced by the VistaDB engine itself.
///             </description>
///         </item>
///         <item>
///             <description>
///                 VistaDB has no <c>IF ... BEGIN ... END</c> idempotent batch primitive — the
///                 <c>GetBeginIf...</c>/<c>GetEndIfScript</c> overrides emit comment markers, and the
///                 per-migration scripts must therefore be applied in order.
///             </description>
///         </item>
///     </list>
///     <para>
///         Original SqlServer body is preserved in a marker block in the source for future revival.
///     </para>
/// </remarks>
public class VistaDBHistoryRepository : HistoryRepository
{
    // VistaDB: no analog — see <remarks>. Original SqlServer body preserved below.
    /*
    // OBJECT_ID + sp_getapplock variant
    protected override string ExistsSql => "SELECT OBJECT_ID(...)" + terminator;
    public override IMigrationsDatabaseLock AcquireDatabaseLock() { sp_getapplock ... }
    public override string GetCreateIfNotExistsScript()
    {
        var builder = new StringBuilder()
            .Append("IF OBJECT_ID(...) IS NULL")
            .AppendLine("BEGIN")
            .AppendLines("    " + GetCreateScript())
            .Append("END");
        return builder.ToString();
    }
    */

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    private readonly IVistaDBDdaAccessor _ddaAccessor;

    public VistaDBHistoryRepository(HistoryRepositoryDependencies dependencies, IVistaDBDdaAccessor ddaAccessor)
        : base(dependencies)
    {
        _ddaAccessor = ddaAccessor;
    }

    /// <summary>
    ///     Never executed — <see cref="Exists" /> is overridden to use DDA because VistaDB has no
    ///     queryable catalog views. Kept because the base class requires the member.
    /// </summary>
    protected override string ExistsSql
        => throw new NotSupportedException(
            "VistaDB has no INFORMATION_SCHEMA/sys catalog views; history-table existence is probed via DDA.");

    /// <inheritdoc />
    protected override bool InterpretExistsResult(object? value)
        => value is not null && value != DBNull.Value && Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture) > 0;

    /// <summary>
    ///     Probes for the migrations-history table. VistaDB rejects <c>INFORMATION_SCHEMA</c>
    ///     (error 627) and DDA's <c>GetTableNames()</c> omits <c>__</c>-prefixed tables (it is meant
    ///     to list user tables, which is what the scaffolder wants), so neither can answer this.
    ///     Instead select from the table directly: success means it exists, and any engine error
    ///     means it does not. A missing database file short-circuits to "not there yet".
    /// </summary>
    public override bool Exists()
    {
        string path = _ddaAccessor.GetDatabaseFilePath();
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return false;
        }

        var sql = new StringBuilder()
            .Append("SELECT COUNT(*) FROM ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(TableName, TableSchema))
            .Append(Dependencies.SqlGenerationHelper.StatementTerminator)
            .ToString();

        try
        {
            var command = Dependencies.RawSqlCommandBuilder.Build(sql);
            command.ExecuteScalar(
                new RelationalCommandParameterObject(
                    Dependencies.Connection,
                    parameterValues: null,
                    readerColumns: null,
                    context: Dependencies.CurrentContext.Context,
                    logger: Dependencies.CommandLogger));
            return true;
        }
        catch (Exception)
        {
            // VistaDB raises rather than returning an empty result when the table is absent.
            return false;
        }
    }

    /// <inheritdoc />
    public override Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Exists());
    }

    /// <inheritdoc />
    public override LockReleaseBehavior LockReleaseBehavior
        => LockReleaseBehavior.Connection;

    /// <inheritdoc />
    public override IMigrationsDatabaseLock AcquireDatabaseLock()
    {
        Dependencies.MigrationsLogger.AcquiringMigrationLock();
        return new VistaDBMigrationDatabaseLock(this);
    }

    /// <inheritdoc />
    public override Task<IMigrationsDatabaseLock> AcquireDatabaseLockAsync(CancellationToken cancellationToken = default)
    {
        Dependencies.MigrationsLogger.AcquiringMigrationLock();
        return Task.FromResult<IMigrationsDatabaseLock>(new VistaDBMigrationDatabaseLock(this));
    }

    /// <inheritdoc />
    public override string GetCreateIfNotExistsScript()
    {
        // VistaDB has no procedural IF/BEGIN/END and no IF NOT EXISTS, so the condition has to be
        // evaluated here rather than expressed in SQL. EF's migrator calls this without checking
        // Exists() first, so emitting an unconditional CREATE makes every subsequent startup fail
        // with "Duplicate table name". Decide now: create only when the table is genuinely absent.
        if (Exists())
        {
            // An empty script is not a parseable statement for the engine, so emit a harmless no-op.
            return "SELECT 1" + Dependencies.SqlGenerationHelper.StatementTerminator;
        }

        var stringTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(string));
        var builder = new StringBuilder();
        builder.Append("-- VistaDB: create of ")
            .Append(stringTypeMapping.GenerateSqlLiteral(TableName))
            .AppendLine(" history table")
            .AppendLine(GetCreateScript());

        return builder.ToString();
    }

    /// <inheritdoc />
    public override string GetBeginIfNotExistsScript(string migrationId)
    {
        // VistaDB has no IF/BEGIN/END SQL. Emit a comment marker; the executor still runs the body which is
        // expected to be idempotent (CREATE TABLE will fail loud, which the migration tool surfaces).
        var stringTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(string));
        return new StringBuilder()
            .Append("-- VistaDB: BEGIN IF NOT EXISTS migration ")
            .Append(stringTypeMapping.GenerateSqlLiteral(migrationId))
            .ToString();
    }

    /// <inheritdoc />
    public override string GetBeginIfExistsScript(string migrationId)
    {
        var stringTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(string));
        return new StringBuilder()
            .Append("-- VistaDB: BEGIN IF EXISTS migration ")
            .Append(stringTypeMapping.GenerateSqlLiteral(migrationId))
            .ToString();
    }

    /// <inheritdoc />
    public override string GetEndIfScript()
        => "-- VistaDB: END IF";
}
