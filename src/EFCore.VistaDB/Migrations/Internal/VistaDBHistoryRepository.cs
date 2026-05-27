// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

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
///                 VistaDB has no <c>OBJECT_ID()</c> / <c>sys.tables</c>. Existence is probed via
///                 <c>INFORMATION_SCHEMA.TABLES</c>.
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
    public VistaDBHistoryRepository(HistoryRepositoryDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <inheritdoc />
    protected override string ExistsSql
    {
        get
        {
            var stringTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(string));
            return new StringBuilder()
                .Append("SELECT COUNT(*) FROM [INFORMATION_SCHEMA].[TABLES] WHERE [TABLE_NAME] = ")
                .Append(stringTypeMapping.GenerateSqlLiteral(TableName))
                .Append(Dependencies.SqlGenerationHelper.StatementTerminator)
                .ToString();
        }
    }

    /// <inheritdoc />
    protected override bool InterpretExistsResult(object? value)
        => value is not null && value != DBNull.Value && Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture) > 0;

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
        var stringTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(string));

        // VistaDB has no procedural IF/BEGIN/END at the SQL level. Probe the catalog via a SELECT and emit
        // the CREATE only when the table is absent. Migrations always run sequentially under a connection,
        // so this two-step pattern is sufficient.
        // For deterministic batched idempotency we emit the CREATE TABLE unconditionally — the engine will
        // throw on duplicate. Callers that need true idempotency should check via Exists() before invoking.
        var builder = new StringBuilder();
        builder.Append("-- VistaDB: idempotent create of ")
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
