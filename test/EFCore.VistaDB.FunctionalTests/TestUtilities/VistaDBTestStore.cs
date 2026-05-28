// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using System.IO;
using VistaDB.Provider;

#nullable enable

namespace Microsoft.EntityFrameworkCore.TestUtilities;

/// <summary>
///     RelationalTestStore adapter for VistaDB. Each store backs onto a temporary <c>.vdb6</c> file under
///     <c>%TEMP%</c> keyed by store name. The file is created on <see cref="InitializeAsync" /> (via
///     <c>EnsureCreated</c>) and removed on <see cref="DisposeAsync" />.
/// </summary>
public class VistaDBTestStore : RelationalTestStore
{
    public const int CommandTimeout = 300;

    public static VistaDBTestStore GetOrCreate(string name)
        => new(name, shared: true);

    public static VistaDBTestStore Create(string name)
        => new(name, shared: false);

    public static async Task<VistaDBTestStore> CreateInitializedAsync(string name)
        => (VistaDBTestStore)await new VistaDBTestStore(name, shared: false)
            .InitializeAsync(null, (Func<DbContext>?)null);

    private readonly string _filePath;

    protected VistaDBTestStore(string name, bool shared)
        : base(name, shared, CreateConnection(name, out var filePath))
    {
        _filePath = filePath;
    }

    public string FilePath
        => _filePath;

    public override DbContextOptionsBuilder AddProviderOptions(DbContextOptionsBuilder builder)
        // Match SqlServerDbContextOptionsBuilderExtensions.ApplyConfiguration: default split-query behavior
        // to SingleQuery. The spec test FixtureBase.AddOptions sets ConfigureWarnings(Default → Throw), so
        // EF Core's default MultipleCollectionIncludeWarning (fired when a multi-collection query is
        // compiled without an explicit QuerySplittingBehavior) becomes a hard exception. SqlServer test
        // fixtures silence it by configuring SingleQuery; we do the same via the VistaDB-specific
        // RelationalDbContextOptionsBuilder.UseQuerySplittingBehavior overload.
        => UseConnectionString
            ? builder.UseVistaDB(ConnectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery))
            : builder.UseVistaDB(Connection, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery));

    protected override async Task InitializeAsync(Func<DbContext> createContext, Func<DbContext, Task>? seed, Func<DbContext, Task>? clean)
    {
        // VistaDB is a file-based engine; the simplest reliable clean-slate is to delete + recreate the file.
        await DeleteFileAsync();

        using var context = createContext();

        if (clean != null)
        {
            await clean(context);
        }

        await context.Database.EnsureCreatedAsync();

        if (seed != null)
        {
            await seed(context);
        }
    }

    /// <summary>
    ///     Truncate all user tables so the next SeedAsync can re-insert without PK collisions.
    ///     SqlServer's <c>SqlServerDatabaseCleaner</c> drops and recreates the database; for VistaDB
    ///     the lightest-weight equivalent is to enumerate tables via the DDA and DELETE FROM each.
    ///     Used by the spec-test <c>ReseedAsync</c> path (notably TransactionTestBase, which inserts
    ///     explicit-Id Customers/Orders on every test iteration). Without this we returned
    ///     <see cref="Task.CompletedTask" />, the seed re-insert hit PK violations, and every
    ///     TransactionVistaDBTest after the first failed.
    /// </summary>
    public override Task CleanAsync(DbContext context)
    {
        if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
        {
            return Task.CompletedTask;
        }

        // Use DDA enumerate-then-delete. ADO.NET DELETE FROM would work too but the DDA path
        // sidesteps connection-state quirks (shared Connection may be open/closed/in-tx).
        try
        {
            using var dda = global::VistaDB.DDA.VistaDBEngine.Connections.OpenDDA();
            using var db = dda.OpenDatabase(_filePath, global::VistaDB.VistaDBDatabaseOpenMode.SingleProcessReadWrite, null);

            // Collect user-table names first, then process; mutating during enumeration is unsafe.
            var tables = new List<string>();
            foreach (var t in db.GetTableNames())
            {
                if (t is string name && !string.IsNullOrEmpty(name))
                {
                    tables.Add(name);
                }
            }

            foreach (var name in tables)
            {
                try
                {
                    using var table = db.OpenTable(name, exclusive: false, readOnly: false);
                    table.First();
                    while (!table.EndOfTable)
                    {
                        table.Delete();
                    }
                }
                catch
                {
                    // best-effort per-table — FK ordering issues, system tables, etc. are tolerable.
                }
            }
        }
        catch
        {
            // best-effort; if DDA can't open (e.g. another open handle), the next SeedAsync will
            // surface a clearer error.
        }

        return Task.CompletedTask;
    }

    public override void OpenConnection()
        => Connection.Open();

    public override Task OpenConnectionAsync()
        => Connection.OpenAsync();

    public int ExecuteNonQuery(string sql, params object[] parameters)
    {
        if (Connection.State != ConnectionState.Open)
        {
            Connection.Open();
        }
        using var command = (VistaDBCommand)Connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = CommandTimeout;
        if (parameters != null)
        {
            for (var i = 0; i < parameters.Length; i++)
            {
                command.Parameters.AddWithValue("@p" + i, parameters[i]);
            }
        }
        return command.ExecuteNonQuery();
    }

    public T ExecuteScalar<T>(string sql, params object[] parameters)
    {
        if (Connection.State != ConnectionState.Open)
        {
            Connection.Open();
        }
        using var command = (VistaDBCommand)Connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = CommandTimeout;
        if (parameters != null)
        {
            for (var i = 0; i < parameters.Length; i++)
            {
                command.Parameters.AddWithValue("@p" + i, parameters[i]);
            }
        }
        return (T)command.ExecuteScalar()!;
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            await Connection.DisposeAsync();
        }
        catch
        {
            // best-effort
        }

        // Shared stores are memoized by name in TestStoreIndex: their InitializeAsync only runs
        // once per test run. If we delete the .vdb6 file on every fixture dispose, subsequent
        // test classes that target the same store name find the file missing — VistaDB engine
        // error 101 "Cannot open data storage or file" — because they skip re-init. Only delete
        // the file for non-shared stores (per-test stores), which are the ones that need
        // clean-slate semantics.
        if (!Shared)
        {
            await DeleteFileAsync();
        }
    }

    private async Task DeleteFileAsync()
    {
        await Task.Yield();
        TryDelete(_filePath);
        TryDelete(_filePath + ".lock");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // best-effort; %TEMP% file
        }
    }

    public static VistaDBConnection CreateConnection(string name, out string filePath)
    {
        filePath = GenerateFilePath(name);
        return new VistaDBConnection($"Data Source={filePath}");
    }

    public static string CreateConnectionString(string name)
        => $"Data Source={GenerateFilePath(name)}";

    private static string GenerateFilePath(string name)
    {
        // Normalize name to a safe filename. Different test stores share a name -> share a file.
        var safe = string.Concat(name.Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_'));
        return Path.Combine(Path.GetTempPath(), $"efcore-vistadb-{safe}.vdb6");
    }
}
