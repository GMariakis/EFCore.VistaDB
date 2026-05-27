// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.EntityFrameworkCore.TestUtilities;
using VistaDB.Provider;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Mirrors EF Core's <c>ExecuteWithStrategyInTransactionAsync</c> pattern as closely as possible:
///     a shared <see cref="VistaDBConnection" /> across multiple iterations, each iteration uses
///     <c>context.Database.BeginTransactionAsync()</c> and lets the <c>using</c> dispose roll back.
///     If iteration 1's SaveChanges DELETE leaks past the outer transaction's rollback, iteration 2
///     sees 0 rows to delete — exactly the GraphUpdates failure pattern.
/// </summary>
public class EfCoreTransactionPatternProbeTest
{
    private readonly ITestOutputHelper _out;

    public EfCoreTransactionPatternProbeTest(ITestOutputHelper @out)
        => _out = @out;

    [VistaDBInstalledFact]
    public async Task SaveChanges_delete_inside_user_tx_rolls_back_when_tx_disposed_without_commit()
    {
        using var file = new TempVistaDBFile();
        // Seed the row OUTSIDE any user transaction (committed).
        using (var ctx = new ProbeContext(file.ConnectionString))
        {
            await ctx.Database.EnsureCreatedAsync();
            ctx.Things.Add(new Thing { Name = "row-1" });
            await ctx.SaveChangesAsync();
        }

        // Shared connection across iterations (mirrors VistaDBTestStore's shared Connection).
        using var conn = new VistaDBConnection($"Data Source={file.FilePath}");

        for (var i = 1; i <= 4; i++)
        {
            using var ctx = new ProbeContextWithSharedConnection(conn);
            using var tx = await ctx.Database.BeginTransactionAsync();

            var thing = ctx.Things.SingleOrDefault(t => t.Name == "row-1");
            if (thing is null)
            {
                _out.WriteLine($"Iteration {i}: row-1 NOT FOUND (rollback didn't restore it)");
                Assert.Fail($"Iteration {i}: row-1 should exist but was not found");
                return;
            }

            ctx.Things.Remove(thing);
            var affected = await ctx.SaveChangesAsync();
            _out.WriteLine($"Iteration {i}: SaveChanges affected {affected} row(s) (deleted row-1)");

            // NO Commit — `using` should dispose the IDbContextTransaction which rolls back.
        }

        _out.WriteLine("All iterations survived. Rollback works under EF Core pattern.");
    }

    private class Thing
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private class ProbeContext : DbContext
    {
        private readonly string _cs;
        public ProbeContext(string cs) => _cs = cs;
        protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseVistaDB(_cs);
        public DbSet<Thing> Things => Set<Thing>();
    }

    private class ProbeContextWithSharedConnection : DbContext
    {
        private readonly VistaDBConnection _conn;
        public ProbeContextWithSharedConnection(VistaDBConnection conn) => _conn = conn;
        protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseVistaDB(_conn);
        public DbSet<Thing> Things => Set<Thing>();
    }
}
