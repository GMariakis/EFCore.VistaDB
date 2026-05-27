// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Reproduces the GraphUpdates fixture seed pattern with EF Core SQL logging enabled, so we
///     can see the exact SQL emitted and isolate why the spec-test fixture's SaveChangesAsync
///     fails with DbUpdateConcurrencyException while the simpler ExplicitIdInsertDiagnosticTest
///     succeeds.
/// </summary>
public class GraphUpdatesSeedDiagnosticTest
{
    private readonly ITestOutputHelper _out;

    public GraphUpdatesSeedDiagnosticTest(ITestOutputHelper @out)
        => _out = @out;

    [VistaDBInstalledFact]
    public async Task TrackGraph_unchanged_then_addrange_with_explicit_ids()
    {
        using var file = new TempVistaDBFile();
        var sqlSink = new StringBuilder();

        using (var ctx = new GraphContext(file.ConnectionString, sqlSink))
        {
            await ctx.Database.EnsureCreatedAsync();
        }

        _out.WriteLine("=== EnsureCreated SQL ===");
        _out.WriteLine(sqlSink.ToString());
        sqlSink.Clear();

        // Mimic GraphUpdatesTestBase.SeedAsync.
        using (var ctx = new GraphContext(file.ConnectionString, sqlSink))
        {
            // Step 1: track an existing-keyed graph as Unchanged (matches KeyValueEntityTracker)
            var existingBloog = new Bloog { Id = 100 };  // pre-existing in DB? actually no, but tracker treats as Unchanged
            ctx.ChangeTracker.TrackGraph(
                existingBloog,
                e =>
                {
                    var entry = e.Entry;
                    var state = entry.IsKeySet ? EntityState.Unchanged : EntityState.Added;
                    _out.WriteLine($"  TrackGraph: {entry.Entity.GetType().Name} IsKeySet={entry.IsKeySet} → {state}");
                    entry.GetInfrastructure().SetEntityState(state, acceptChanges: true);
                });

            // Step 2: add a real explicit-Id entity (this is the Added path)
            var bloog = new Bloog { Id = 515 };
            ctx.AddRange(
                new Poost { Id = 516, Bloog = bloog },
                new Poost { Id = 517, Bloog = bloog });

            _out.WriteLine("");
            _out.WriteLine("=== Entries before SaveChanges ===");
            foreach (var entry in ctx.ChangeTracker.Entries())
            {
                _out.WriteLine($"  {entry.Entity.GetType().Name}({GetIdValue(entry)}) — {entry.State}");
            }

            _out.WriteLine("");
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
                    _out.WriteLine($"  Affected entry: {entry.Entity.GetType().Name}({GetIdValue(entry)}) state={entry.State}");
                }
            }
        }

        _out.WriteLine("");
        _out.WriteLine("=== Emitted SQL ===");
        _out.WriteLine(sqlSink.ToString());
    }

    private static int? GetIdValue(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var idProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
        return idProp?.CurrentValue as int?;
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

        public DbSet<Bloog> Bloogs => Set<Bloog>();
        public DbSet<Poost> Poosts => Set<Poost>();

        protected override void OnConfiguring(DbContextOptionsBuilder o)
            => o.UseVistaDB(_cs)
                .LogTo(s => _sqlSink.AppendLine(s),
                    new[] { DbLoggerCategory.Database.Command.Name },
                    LogLevel.Information);
    }

    private class Bloog
    {
        public int Id { get; set; }
        public ICollection<Poost> Poosts { get; set; } = new List<Poost>();
    }

    private class Poost
    {
        public int Id { get; set; }
        public int BloogId { get; set; }
        public Bloog Bloog { get; set; } = null!;
    }
}
