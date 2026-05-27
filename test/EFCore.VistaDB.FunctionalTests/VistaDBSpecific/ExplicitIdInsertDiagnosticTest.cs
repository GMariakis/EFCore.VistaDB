// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Simulates the GraphUpdatesTestBase fixture pattern: AddRange entities with explicit Id values
///     on an IDENTITY column, then SaveChangesAsync. With Fix A's
///     <see cref="VistaDB.Update.Internal.VistaDBModificationCommandBatch" /> IDENTITY_INSERT wrapping,
///     SaveChanges must succeed and the explicit Ids must persist.
/// </summary>
public class ExplicitIdInsertDiagnosticTest
{
    private readonly ITestOutputHelper _out;

    public ExplicitIdInsertDiagnosticTest(ITestOutputHelper @out)
        => _out = @out;

    [VistaDBInstalledFact]
    public async Task AddRange_with_explicit_ids_on_identity_column_succeeds()
    {
        using var file = new TempVistaDBFile();
        var connectionString = file.ConnectionString;

        // Seed phase.
        using (var ctx = new BloogContext(connectionString))
        {
            await ctx.Database.EnsureCreatedAsync();

            var bloog = new Bloog { Id = 515 };
            ctx.AddRange(
                new Poost { Id = 516, Bloog = bloog },
                new Poost { Id = 517, Bloog = bloog });

            var affected = await ctx.SaveChangesAsync();
            _out.WriteLine($"SaveChangesAsync returned {affected}");
            Assert.Equal(3, affected); // 1 Bloog + 2 Poosts
        }

        // Verification phase: explicit Ids survived.
        using (var ctx = new BloogContext(connectionString))
        {
            var bloog = await ctx.Bloogs.FindAsync(515);
            Assert.NotNull(bloog);

            var poost516 = await ctx.Poosts.FindAsync(516);
            var poost517 = await ctx.Poosts.FindAsync(517);
            Assert.NotNull(poost516);
            Assert.NotNull(poost517);
            _out.WriteLine($"Found Bloog(Id=515), Poost(Id=516), Poost(Id=517) — all explicit Ids persisted.");
        }
    }

    private class BloogContext : DbContext
    {
        private readonly string _cs;

        public BloogContext(string cs)
            => _cs = cs;

        public DbSet<Bloog> Bloogs
            => Set<Bloog>();

        public DbSet<Poost> Poosts
            => Set<Poost>();

        protected override void OnConfiguring(DbContextOptionsBuilder o)
            => o.UseVistaDB(_cs);
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
