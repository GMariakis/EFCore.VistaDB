// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBConnection : RelationalConnection, IVistaDBConnection
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBConnection(RelationalConnectionDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    /// <remarks>
    ///     We augment the user-supplied connection string with <c>Open Mode=MultiProcessReadWrite</c> when
    ///     the caller hasn't already chosen an explicit mode. <c>.vdb6</c> files can be opened many times —
    ///     by EF Core's ADO.NET pipeline, by parallel DDA handles (<see cref="IVistaDBDdaAccessor" /> for
    ///     Tier-2 operations like IDENTITY_INSERT), and by the scaffolding model factory's introspection
    ///     queries. <c>SingleProcessReadWrite</c> blocks the DDA handle from opening alongside the ADO.NET
    ///     connection (VistaDB error 219). <c>MultiProcessReadWrite</c> allows all three to coexist.
    ///     Users who want stricter exclusivity can specify <c>Open Mode=SingleProcessReadWrite</c> or
    ///     <c>Open Mode=ExclusiveReadWrite</c> explicitly in their connection string and the override
    ///     below leaves it untouched.
    /// </remarks>
    protected override DbConnection CreateDbConnection()
        => new global::VistaDB.Provider.VistaDBConnection(AugmentConnectionString(GetValidatedConnectionString()));

    private static string AugmentConnectionString(string connectionString)
    {
        var builder = new global::VistaDB.Provider.VistaDBConnectionStringBuilder(connectionString);

        // Only set Open Mode if the caller didn't already specify it. The connection-string keyword
        // is "Open Mode" (with the space) — VistaDBConnectionStringBuilder.OpenMode is the typed alias.
        if (!builder.ContainsKey("Open Mode"))
        {
            builder.OpenMode = global::VistaDB.VistaDBDatabaseOpenMode.MultiProcessReadWrite;
        }

        return builder.ConnectionString;
    }

    /// <summary>
    ///     Indicates whether the store connection supports ambient transactions.
    /// </summary>
    protected override bool SupportsAmbientTransactions
        => true;

    // VistaDB: no analog — VistaDB has no "master" database; each database is a self-contained .vdb6 file.
    // SqlServer's Application Name connection-string injection and MARS detection also do not apply.
    // Original SqlServer logic preserved below for future revival when VistaDB grows analogs.
    /*
        // Compensate for slow SQL Server database creation
        private const int DefaultMasterConnectionCommandTimeout = 60;

        private static readonly ConcurrentDictionary<string, bool> MultipleActiveResultSetsEnabledMap = new();

        private static readonly ConcurrentDictionary<string, string> ConnectionStringMap = new();

        private static string? _defaultApplicationName;

        public override string? ConnectionString
        {
            get => base.ConnectionString;
            set => base.ConnectionString = value is null ? null : ConnectionStringMap.GetOrAdd(value, ModifyConnectionString);
        }

        protected override void OpenDbConnection(bool errorsExpected)
        {
            if (errorsExpected
                && DbConnection is SqlConnection sqlConnection)
            {
                sqlConnection.Open(SqlConnectionOverrides.OpenWithoutRetry);
            }
            else
            {
                DbConnection.Open();
            }
        }

        protected override Task OpenDbConnectionAsync(bool errorsExpected, CancellationToken cancellationToken)
        {
            if (errorsExpected
                && DbConnection is SqlConnection sqlConnection)
            {
                return sqlConnection.OpenAsync(SqlConnectionOverrides.OpenWithoutRetry, cancellationToken);
            }

            return DbConnection.OpenAsync(cancellationToken);
        }

        public virtual ISqlServerConnection CreateMasterConnection()
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder(GetValidatedConnectionString()) { InitialCatalog = "master" };
            connectionStringBuilder.Remove("AttachDBFilename");

            var contextOptions = new DbContextOptionsBuilder()
                .UseSqlServer(
                    connectionStringBuilder.ConnectionString,
                    b => b.CommandTimeout(CommandTimeout ?? DefaultMasterConnectionCommandTimeout))
                .Options;

            return new SqlServerConnection(Dependencies with { ContextOptions = contextOptions });
        }

        public virtual bool IsMultipleActiveResultSetsEnabled
        {
            get
            {
                var connectionString = ConnectionString;

                return connectionString != null
                    && MultipleActiveResultSetsEnabledMap.GetOrAdd(
                        connectionString, cs => new SqlConnectionStringBuilder(cs).MultipleActiveResultSets);
            }
        }

        protected virtual string ModifyConnectionString(string userProvidedConnectionString)
        {
            ...
        }
    */
}
