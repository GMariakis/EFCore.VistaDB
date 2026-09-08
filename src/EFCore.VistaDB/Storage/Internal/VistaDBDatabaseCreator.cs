// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using VistaDB.DDA;

namespace Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
/// <remarks>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </remarks>
public class VistaDBDatabaseCreator(
    RelationalDatabaseCreatorDependencies dependencies,
    IVistaDBConnection connection,
    IRawSqlCommandBuilder rawSqlCommandBuilder) : RelationalDatabaseCreator(dependencies)
{
    private readonly IVistaDBConnection _connection = connection;
    private readonly IRawSqlCommandBuilder _rawSqlCommandBuilder = rawSqlCommandBuilder;

    // Pass 0 for page size so the VistaDB engine uses its own default (per the VistaDB.6 xmldoc:
    // "If 0 is passed default page size is used"). The previous value 4096 was a unit-confusion
    // bug — the parameter is *kilobytes*, not bytes, so we were asking for a 4 MB page size and
    // ballooning every newly-created file by ~1024×. LCID 1033 = en-US (engine default for us).
    private const int DefaultPageSize = 0;
    private const int DefaultLocaleId = 0x0409;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override void Create()
    {
        var fileName = GetDatabaseFileName();

        // Use the 7-parameter CreateDatabase overload with staySingleProcess: false so the new file
        // is multi-process compatible from the start. The 6-parameter overload that doesn't take
        // staySingleProcess in some VistaDB versions defaults to single-process mode despite the
        // documentation claiming otherwise, which then blocks the scaffolding factory's parallel
        // DDA open with error 219.
        using (var dda = VistaDBEngine.Connections.OpenDDA())
        using (var database = dda.CreateDatabase(
                   fileName,
                   stayExclusive: false,
                   staySingleProcess: false,
                   encryptionKeyString: null,
                   pageSize: DefaultPageSize,
                   LCID: DefaultLocaleId,
                   caseSensitive: false))
        {
            database.Close();
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override Task CreateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Create();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override bool Exists()
    {
        var fileName = GetDatabaseFileName();
        return !string.IsNullOrEmpty(fileName) && File.Exists(fileName);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Exists());
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override void Delete()
    {
        var fileName = GetDatabaseFileName();
        if (string.IsNullOrEmpty(fileName) || !File.Exists(fileName))
        {
            return;
        }

        // Close the ADO.NET connection so the engine drops its cached database handle for this file.
        try
        {
            _connection.Close();
        }
        catch
        {
            // Best-effort; we'll force-clear below.
        }

        // The VistaDB engine caches database handles in a static Connections collection. Without
        // clearing it, subsequent File.Delete fails with "process cannot access the file" because
        // the engine still has the file open. Mirror what the SqlServer provider does via
        // SqlConnection.ClearAllPools().
        try
        {
            VistaDBEngine.Connections.Clear();
        }
        catch
        {
            // If Clear() throws there's nothing we can do but try the delete anyway.
        }

        File.Delete(fileName);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Delete();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override bool HasTables()
    {
        var fileName = GetDatabaseFileName();
        if (string.IsNullOrEmpty(fileName) || !File.Exists(fileName))
        {
            return false;
        }

        using var dda = VistaDBEngine.Connections.OpenDDA();
        // The read-only sibling of whatever family the connection is in. A probe from another family
        // fails against a .vdb6 EF Core already has open — which is what this comment used to describe
        // while naming a mode the connection had stopped using. See VistaDBOpenModes.
        using var database = dda.OpenDatabase(
            fileName, VistaDBOpenModes.ForDda(_connection.DbConnection.ConnectionString, readOnly: true), null);
        var hasTables = false;
        foreach (var tableName in database.GetTableNames())
        {
            if (!string.IsNullOrEmpty(tableName as string))
            {
                hasTables = true;
                break;
            }
        }

        database.Close();
        return hasTables;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override Task<bool> HasTablesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(HasTables());
    }

    private string GetDatabaseFileName()
    {
        // VistaDB connection strings use "DataSource=<path>" (or "Data Source") to point at a .vdb6 file.
        var dataSource = _connection.DbConnection.DataSource;
        return dataSource ?? string.Empty;
    }

    // VistaDB: no analog — VistaDB has no "master" database, no DROP DATABASE / CREATE DATABASE SQL,
    // and no SqlClient connection-pool API. File-system operations replace these.
    // Original SqlServer logic preserved below for future revival.
    /*
        public virtual TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(500);
        public virtual TimeSpan RetryTimeout { get; set; } = TimeSpan.FromMinutes(1);

        public override void Create()
        {
            using (var masterConnection = _connection.CreateMasterConnection())
            {
                Dependencies.MigrationCommandExecutor
                    .ExecuteNonQuery(CreateCreateOperations(), masterConnection, new MigrationExecutionState(), commitTransaction: true);

                ClearPool();
            }

            Exists(retryOnNotExists: true);
        }

        public override async Task CreateAsync(CancellationToken cancellationToken = default) { ... }

        public override bool HasTables()
            => Dependencies.ExecutionStrategy.Execute(
                _connection,
                connection => (int)CreateHasTablesCommand()
                        .ExecuteScalar(...)
                    != 0,
                null);

        private IRelationalCommand CreateHasTablesCommand()
            => _rawSqlCommandBuilder.Build(@"IF EXISTS (...) SELECT 1 ELSE SELECT 0");

        private IReadOnlyList<MigrationCommand> CreateCreateOperations()
        {
            var builder = new SqlConnectionStringBuilder(_connection.DbConnection.ConnectionString);
            return Dependencies.MigrationsSqlGenerator.Generate(
            [
                new SqlServerCreateDatabaseOperation
                {
                    Name = builder.InitialCatalog,
                    FileName = builder.AttachDBFilename,
                    Collation = Dependencies.CurrentContext.Context.GetService<IDesignTimeModel>()
                        .Model.GetRelationalModel().Collation
                }
            ]);
        }

        public override bool Exists() => Exists(retryOnNotExists: false);

        private bool Exists(bool retryOnNotExists)
            => Dependencies.ExecutionStrategy.Execute(
                DateTime.UtcNow + RetryTimeout, giveUp => { ... }, null);

        private static bool IsDoesNotExist(SqlException exception)
            => exception.Number is 4060 or 1832 or 5120;

        private bool RetryOnExistsFailure(SqlException exception) { ... }

        public override void Delete()
        {
            ClearAllPools();

            using var masterConnection = _connection.CreateMasterConnection();
            Dependencies.MigrationCommandExecutor
                .ExecuteNonQuery(CreateDropCommands(), masterConnection, new MigrationExecutionState(), commitTransaction: true);
        }

        private IReadOnlyList<MigrationCommand> CreateDropCommands()
        {
            var databaseName = _connection.DbConnection.Database;
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new InvalidOperationException(SqlServerStrings.NoInitialCatalog);
            }

            var operations = new MigrationOperation[] { new SqlServerDropDatabaseOperation { Name = databaseName } };
            return Dependencies.MigrationsSqlGenerator.Generate(operations);
        }

        private static void ClearAllPools() => SqlConnection.ClearAllPools();
        private void ClearPool() => SqlConnection.ClearPool((SqlConnection)_connection.DbConnection);
    */
}
