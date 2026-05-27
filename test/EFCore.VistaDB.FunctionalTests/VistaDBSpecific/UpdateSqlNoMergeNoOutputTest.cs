// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     VistaDB: no analog — MERGE and OUTPUT are SqlServer-only. These tests assert the VistaDB
///     update-SQL generator never emits either token, even under multi-row insert/update/delete
///     scenarios that would be batched on SqlServer.
/// </summary>
public class UpdateSqlNoMergeNoOutputTest
{
    [VistaDBInstalledFact]
    public async Task Single_insert_with_identity_does_not_emit_MERGE_or_OUTPUT()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("UpdateSqlInsertOne");
        var logger = new SqlCapturingLoggerFactory();
        using var ctx = new MergeContext(store.ConnectionString, logger);
        ctx.Database.EnsureCreated();
        logger.Clear();

        ctx.Items.Add(new Item { Name = "a" });
        ctx.SaveChanges();

        Assert.DoesNotContain(logger.Statements, s => s.Contains("MERGE", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(logger.Statements, s => s.Contains("OUTPUT", StringComparison.OrdinalIgnoreCase));
    }

    [VistaDBInstalledFact]
    public async Task Multiple_inserts_remain_per_row_no_MERGE_batching()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("UpdateSqlInsertMany");
        var logger = new SqlCapturingLoggerFactory();
        using var ctx = new MergeContext(store.ConnectionString, logger);
        ctx.Database.EnsureCreated();
        logger.Clear();

        ctx.Items.AddRange(new Item { Name = "a" }, new Item { Name = "b" }, new Item { Name = "c" });
        ctx.SaveChanges();

        Assert.DoesNotContain(logger.Statements, s => s.Contains("MERGE", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(logger.Statements, s => s.Contains("OUTPUT", StringComparison.OrdinalIgnoreCase));
    }

    [VistaDBInstalledFact]
    public async Task Update_does_not_emit_MERGE_or_OUTPUT()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("UpdateSqlUpdate");
        var logger = new SqlCapturingLoggerFactory();
        using var ctx = new MergeContext(store.ConnectionString, logger);
        ctx.Database.EnsureCreated();

        var item = new Item { Name = "before" };
        ctx.Items.Add(item);
        ctx.SaveChanges();
        logger.Clear();

        item.Name = "after";
        ctx.SaveChanges();

        Assert.DoesNotContain(logger.Statements, s => s.Contains("MERGE", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(logger.Statements, s => s.Contains("OUTPUT", StringComparison.OrdinalIgnoreCase));
    }

    [VistaDBInstalledFact]
    public async Task Delete_does_not_emit_MERGE_or_OUTPUT()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("UpdateSqlDelete");
        var logger = new SqlCapturingLoggerFactory();
        using var ctx = new MergeContext(store.ConnectionString, logger);
        ctx.Database.EnsureCreated();

        var item = new Item { Name = "doomed" };
        ctx.Items.Add(item);
        ctx.SaveChanges();
        logger.Clear();

        ctx.Items.Remove(item);
        ctx.SaveChanges();

        Assert.DoesNotContain(logger.Statements, s => s.Contains("MERGE", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(logger.Statements, s => s.Contains("OUTPUT", StringComparison.OrdinalIgnoreCase));
    }

    [VistaDBInstalledFact]
    public async Task Insert_emits_SCOPE_IDENTITY_for_identity_PK()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("UpdateSqlScopeIdentity");
        var logger = new SqlCapturingLoggerFactory();
        using var ctx = new MergeContext(store.ConnectionString, logger);
        ctx.Database.EnsureCreated();
        logger.Clear();

        ctx.Items.Add(new Item { Name = "a" });
        ctx.SaveChanges();

        Assert.Contains(
            logger.Statements,
            s => s.Contains("SCOPE_IDENTITY", StringComparison.OrdinalIgnoreCase));
    }

    [VistaDBInstalledFact]
    public async Task Update_emits_ROWCOUNT_check()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("UpdateSqlRowcount");
        var logger = new SqlCapturingLoggerFactory();
        using var ctx = new MergeContext(store.ConnectionString, logger);
        ctx.Database.EnsureCreated();

        var item = new Item { Name = "x" };
        ctx.Items.Add(item);
        ctx.SaveChanges();
        logger.Clear();

        item.Name = "y";
        ctx.SaveChanges();

        Assert.Contains(
            logger.Statements,
            s => s.Contains("@@ROWCOUNT", StringComparison.OrdinalIgnoreCase));
    }

    private class MergeContext : DbContext
    {
        private readonly string _cs;
        private readonly ILoggerFactory _factory;

        public MergeContext(string cs, ILoggerFactory factory)
        {
            _cs = cs;
            _factory = factory;
        }

        public DbSet<Item> Items { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder builder)
            => builder
                .UseVistaDB(_cs)
                .UseLoggerFactory(_factory);
    }

    private class Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    ///     Tiny logger factory that captures all SQL statements written by EF Core for assertion.
    /// </summary>
    private sealed class SqlCapturingLoggerFactory : ILoggerFactory
    {
        private readonly List<string> _statements = new();

        public IReadOnlyList<string> Statements
            => _statements;

        public void Clear()
            => _statements.Clear();

        public ILogger CreateLogger(string categoryName)
            => new CapturingLogger(_statements);

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }

        private sealed class CapturingLogger : ILogger
        {
            private readonly List<string> _statements;

            public CapturingLogger(List<string> statements)
            {
                _statements = statements;
            }

            public IDisposable BeginScope<TState>(TState state)
                where TState : notnull
                => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel)
                => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception exception,
                Func<TState, Exception, string> formatter)
            {
                if (eventId.Id == 20100 /* Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted */)
                {
                    _statements.Add(formatter(state, exception) ?? string.Empty);
                }
            }

            private sealed class NullScope : IDisposable
            {
                public static readonly NullScope Instance = new();
                public void Dispose() { }
            }
        }
    }
}
