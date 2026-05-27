// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Closer replica of GraphUpdatesTestBase.SeedAsync — combines (a) tracked-Unchanged graph
///     entities, (b) auto-Identity Add()s, and (c) explicit-Identity AddRange()s in a single
///     SaveChangesAsync. Also uses <c>long</c> primary keys to match the SharedFkRoot /
///     SharedFkParent / SharedFkDependant trio that appear in the real seed. Captures the
///     entire SQL log so we can spot which command's SELECT-back returns 0 rows and trips
///     DbUpdateConcurrencyException.
/// </summary>
public class MixedSeedDiagnosticTest
{
    private readonly ITestOutputHelper _out;

    public MixedSeedDiagnosticTest(ITestOutputHelper @out)
        => _out = @out;

    [VistaDBInstalledFact]
    public async Task TrackGraph_unchanged_plus_auto_and_explicit_id_adds_in_one_savechanges()
    {
        using var file = new TempVistaDBFile();
        var sqlSink = new StringBuilder();

        using (var ctx = new SeedContext(file.ConnectionString, sqlSink))
        {
            await ctx.Database.EnsureCreatedAsync();
        }

        sqlSink.Clear();

        using (var ctx = new SeedContext(file.ConnectionString, sqlSink))
        {
            // (a) tracked-as-Unchanged — these entities have set keys but were NOT in the DB.
            //     Mimics CreateFullGraph's "preload tracker" with KeyValueEntityTracker.
            //     IsKeySet -> Unchanged.
            var tracked = new Bloog { Id = 100 };
            ctx.ChangeTracker.TrackGraph(
                tracked,
                e =>
                {
                    var entry = e.Entry;
                    var state = entry.IsKeySet ? EntityState.Unchanged : EntityState.Added;
                    entry.GetInfrastructure().SetEntityState(state, acceptChanges: true);
                });

            // (b) auto-Identity Adds (SQL path) — Bigly is long-keyed like SharedFkRoot
            var biglyRoot = new Bigly();
            ctx.Add(biglyRoot);
            ctx.Add(new BiglyParent { Root = biglyRoot });

            // (c) explicit-Identity AddRange (DDA-2 path) — Bloog/Poost as in real seed
            var bloog = new Bloog { Id = 515 };
            ctx.AddRange(
                new Poost { Id = 516, Bloog = bloog },
                new Poost { Id = 517, Bloog = bloog });

            _out.WriteLine("=== Entries before SaveChanges ===");
            foreach (var entry in ctx.ChangeTracker.Entries())
            {
                _out.WriteLine($"  {entry.Entity.GetType().Name} — {entry.State}");
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
                    _out.WriteLine($"  Failed entry: {entry.Entity.GetType().Name} state={entry.State}");
                }
                _out.WriteLine("");
                _out.WriteLine("=== SQL emitted up to failure ===");
                _out.WriteLine(sqlSink.ToString());
                throw;
            }
        }

        _out.WriteLine("=== Full SQL log ===");
        _out.WriteLine(sqlSink.ToString());
    }

    private class SeedContext : DbContext
    {
        private readonly string _cs;
        private readonly StringBuilder _sqlSink;

        public SeedContext(string cs, StringBuilder sqlSink)
        {
            _cs = cs;
            _sqlSink = sqlSink;
        }

        public DbSet<Bloog> Bloogs => Set<Bloog>();
        public DbSet<Poost> Poosts => Set<Poost>();
        public DbSet<Bigly> Biglys => Set<Bigly>();
        public DbSet<BiglyParent> BiglyParents => Set<BiglyParent>();

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

    private class Bigly
    {
        public long Id { get; set; }
        public ICollection<BiglyParent> Parents { get; } = new List<BiglyParent>();
    }

    private class BiglyParent
    {
        public long Id { get; set; }
        public long RootId { get; set; }
        public Bigly Root { get; set; } = null!;
    }
}
