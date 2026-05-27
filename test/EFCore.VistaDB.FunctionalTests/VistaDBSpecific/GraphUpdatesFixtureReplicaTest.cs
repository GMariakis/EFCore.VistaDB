// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Replicates a minimal slice of the GraphUpdatesTestBase.SeedAsync pattern WITHOUT the
///     KeyValueEntityTracker — purely <c>context.Add(...)</c> with default Identity Ids on an
///     entity graph that has FK relationships. Captures the emitted SQL so we can pinpoint what
///     differs between the simple Bloog/Poost diagnostic (which passes) and the full GraphUpdates
///     fixture seed (which fails with DbUpdateConcurrencyException).
/// </summary>
public class GraphUpdatesFixtureReplicaTest
{
    private readonly ITestOutputHelper _out;

    public GraphUpdatesFixtureReplicaTest(ITestOutputHelper @out)
        => _out = @out;

    [VistaDBInstalledFact]
    public async Task Add_graph_with_default_identity_ids_and_fk_chains()
    {
        using var file = new TempVistaDBFile();
        var sqlSink = new StringBuilder();

        using (var ctx = new GraphContext(file.ConnectionString, sqlSink))
        {
            await ctx.Database.EnsureCreatedAsync();
        }

        _out.WriteLine($"--- EnsureCreated SQL log lines: {sqlSink.ToString().Split('\n').Length}");
        sqlSink.Clear();

        using (var ctx = new GraphContext(file.ConnectionString, sqlSink))
        {
            // Root with default Id, multiple required children with default Ids; mirrors the
            // CreateFullGraph pattern but tiny.
            var root = new Root
            {
                RequiredChildren =
                {
                    new Required1 { Children = { new Required2(), new Required2() } },
                    new Required1 { Children = { new Required2(), new Required2() } },
                }
            };
            ctx.Add(root);

            _out.WriteLine($"Entries: {ctx.ChangeTracker.Entries().Count()}");
            foreach (var entry in ctx.ChangeTracker.Entries())
            {
                _out.WriteLine($"  {entry.Entity.GetType().Name} — {entry.State}");
            }

            try
            {
                var affected = await ctx.SaveChangesAsync();
                _out.WriteLine($"SaveChanges OK — affected={affected}");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _out.WriteLine($"DbUpdateConcurrencyException: {ex.Message}");
                foreach (var entry in ex.Entries)
                {
                    _out.WriteLine($"  Affected: {entry.Entity.GetType().Name} state={entry.State}");
                }

                throw;
            }
        }

        _out.WriteLine("=== Emitted SQL ===");
        foreach (var line in sqlSink.ToString().Split('\n'))
        {
            if (line.Contains("Executed DbCommand") || line.TrimStart().StartsWith("INSERT") || line.TrimStart().StartsWith("SELECT") || line.TrimStart().StartsWith("SET "))
            {
                _out.WriteLine("  " + line.TrimEnd());
            }
        }
    }

    private class GraphContext : DbContext
    {
        private readonly string _cs;
        private readonly StringBuilder _sqlSink;

        public GraphContext(string cs, StringBuilder sqlSink)
        {
            _cs = cs;
            _sqlSink = sqlSink;
        }

        public DbSet<Root> Roots => Set<Root>();
        public DbSet<Required1> Required1s => Set<Required1>();
        public DbSet<Required2> Required2s => Set<Required2>();

        protected override void OnConfiguring(DbContextOptionsBuilder o)
            => o.UseVistaDB(_cs)
                .LogTo(s => _sqlSink.AppendLine(s),
                    new[] { DbLoggerCategory.Database.Command.Name },
                    LogLevel.Information);
    }

    private class Root
    {
        public int Id { get; set; }
        public ICollection<Required1> RequiredChildren { get; } = new List<Required1>();
    }

    private class Required1
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public Root Parent { get; set; } = null!;
        public ICollection<Required2> Children { get; } = new List<Required2>();
    }

    private class Required2
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public Required1 Parent { get; set; } = null!;
    }
}
