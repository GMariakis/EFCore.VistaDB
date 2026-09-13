// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDB.Migrations.Internal;

/// <summary>
///     VistaDB migration history uses the documented [database schema] catalog and conditional SQL.
///     This is an internal EF Core provider API.
/// </summary>
public class VistaDBHistoryRepository : HistoryRepository
{
    private readonly IVistaDBDdaAccessor _ddaAccessor;

    public VistaDBHistoryRepository(HistoryRepositoryDependencies dependencies, IVistaDBDdaAccessor ddaAccessor)
        : base(dependencies)
        => _ddaAccessor = ddaAccessor;

    // The public existence methods also handle a missing database file. The legacy protected
    // member remains unsupported; neither existence method delegates to the base implementation.
    protected override string ExistsSql
        => throw new NotSupportedException("Use the VistaDB history Exists or ExistsAsync implementation.");

    protected override bool InterpretExistsResult(object? value)
        => value is not null && value != DBNull.Value
            && Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture) > 0;

    private string CatalogPredicate
        => "[typeid] = 1 AND [name] = "
            + Dependencies.TypeMappingSource.GetMapping(typeof(string)).GenerateSqlLiteral(
                VistaDBSqlGenerationHelper.SanitizeForVistaDB(TableName));

    private string CatalogExistsSql
        => "SELECT COUNT(*) FROM [database schema] WHERE " + CatalogPredicate
            + Dependencies.SqlGenerationHelper.StatementTerminator;

    private bool DatabaseFileExists()
    {
        var path = _ddaAccessor.GetDatabaseFilePath();
        return !string.IsNullOrEmpty(path) && File.Exists(path);
    }

    private RelationalCommandParameterObject CommandParameters
        => new(Dependencies.Connection, null, null, Dependencies.CurrentContext.Context, Dependencies.CommandLogger);

    public override bool Exists()
        => DatabaseFileExists()
            && InterpretExistsResult(Dependencies.RawSqlCommandBuilder.Build(CatalogExistsSql).ExecuteScalar(CommandParameters));

    public override async Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return DatabaseFileExists()
            && InterpretExistsResult(await Dependencies.RawSqlCommandBuilder.Build(CatalogExistsSql)
                .ExecuteScalarAsync(CommandParameters, cancellationToken).ConfigureAwait(false));
    }

    public override LockReleaseBehavior LockReleaseBehavior
        => LockReleaseBehavior.Explicit;

    public override IMigrationsDatabaseLock AcquireDatabaseLock()
    {
        Dependencies.MigrationsLogger.AcquiringMigrationLock();
        return VistaDBMigrationDatabaseLock.Acquire(this, _ddaAccessor.GetDatabaseFilePath(), Dependencies.Connection.CommandTimeout ?? 30, async: false).GetAwaiter().GetResult();
    }

    public override async Task<IMigrationsDatabaseLock> AcquireDatabaseLockAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Dependencies.MigrationsLogger.AcquiringMigrationLock();
        return await VistaDBMigrationDatabaseLock.Acquire(this, _ddaAccessor.GetDatabaseFilePath(), Dependencies.Connection.CommandTimeout ?? 30, async: true, cancellationToken).ConfigureAwait(false);
    }

    public override string GetCreateIfNotExistsScript()
        => new StringBuilder()
            .AppendLine("-- VistaDB: create of history table")
            .Append("IF NOT EXISTS (SELECT 1 FROM [database schema] WHERE ").Append(CatalogPredicate).AppendLine(")")
            .AppendLine("BEGIN")
            .AppendLine(GetCreateScript())
            .Append("END").AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
            .ToString();

    public override string GetBeginIfNotExistsScript(string migrationId)
        => GetBeginIfScript(migrationId, notExists: true);

    public override string GetBeginIfExistsScript(string migrationId)
        => GetBeginIfScript(migrationId, notExists: false);

    private string GetBeginIfScript(string migrationId, bool notExists)
    {
        var condition = notExists ? "NOT EXISTS" : "EXISTS";
        return new StringBuilder()
            .Append("-- VistaDB: BEGIN IF ").Append(condition).AppendLine(" migration")
            .Append("IF ").Append(condition).Append(" (SELECT 1 FROM ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(TableName, TableSchema))
            .Append(" WHERE ").Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(MigrationIdColumnName))
            .Append(" = ").Append(Dependencies.TypeMappingSource.GetMapping(typeof(string)).GenerateSqlLiteral(migrationId))
            .AppendLine(")")
            .AppendLine("BEGIN")
            .ToString();
    }

    public override string GetEndIfScript()
        => "END" + Dependencies.SqlGenerationHelper.StatementTerminator + Environment.NewLine + "-- VistaDB: END IF";
}
