// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;

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
    ///     We augment the user-supplied connection string with <see cref="VistaDBOpenModes.Default" />
    ///     when the caller hasn't already chosen an explicit mode. A <c>.vdb6</c> is opened several ways
    ///     at once — this connection, the DDA handles used for Tier-2 work, and the scaffolding factory's
    ///     introspection — and every one of them has to be in the same locking family or the engine
    ///     refuses. Those other openers derive their mode from this connection string, so a caller who
    ///     names <c>Open Mode</c> explicitly moves all of them together and this override leaves it
    ///     untouched.
    /// </remarks>
    protected override DbConnection CreateDbConnection()
        => new global::VistaDB.Provider.VistaDBConnection(AugmentConnectionString(GetValidatedConnectionString()));

    /// <summary>Test seam: the augmentation is the part that silently did nothing.</summary>
    public static string AugmentConnectionStringForTesting(string connectionString)
        => AugmentConnectionString(connectionString);

    private static string AugmentConnectionString(string connectionString)
    {
        // Ask a plain DbConnectionStringBuilder whether the caller named a mode, not VistaDB's own.
        // VistaDBConnectionStringBuilder pre-populates its strongly-typed keywords, so ContainsKey
        // returns true for "Open Mode" on a string that never mentioned it — which silently skipped
        // this whole method and left every connection on the engine default of SingleProcess. The
        // base builder only holds keys that are actually present.
        var supplied = new DbConnectionStringBuilder { ConnectionString = connectionString };
        if (supplied.ContainsKey(VistaDBOpenModes.Keyword))
        {
            return connectionString;
        }

        return new global::VistaDB.Provider.VistaDBConnectionStringBuilder(connectionString)
        {
            OpenMode = VistaDBOpenModes.Default,
        }.ConnectionString;
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
