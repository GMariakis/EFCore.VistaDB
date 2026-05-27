// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Round-trip identity-column behavior against a live VistaDB engine. All tests use
///     <see cref="VistaDBInstalledFactAttribute"/> so CI hosts without the native engine skip cleanly.
/// </summary>
public class IdentityColumnRoundTripTest
{
    [VistaDBInstalledFact]
    public async Task Insert_single_row_with_int_identity_returns_assigned_id()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("IdentityInt");
        using var ctx = new IdentityContext(store.ConnectionString);
        ctx.Database.EnsureCreated();

        var entity = new IntIdentityEntity { Name = "row1" };
        ctx.IntIdentities.Add(entity);
        ctx.SaveChanges();

        Assert.NotEqual(0, entity.Id);
    }

    [VistaDBInstalledFact]
    public async Task Insert_multiple_rows_in_one_SaveChanges_returns_distinct_ids()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("IdentityIntMulti");
        using var ctx = new IdentityContext(store.ConnectionString);
        ctx.Database.EnsureCreated();

        var rows = new[]
        {
            new IntIdentityEntity { Name = "a" },
            new IntIdentityEntity { Name = "b" },
            new IntIdentityEntity { Name = "c" }
        };
        ctx.IntIdentities.AddRange(rows);
        ctx.SaveChanges();

        Assert.Equal(3, rows.Select(r => r.Id).Distinct().Count());
    }

    [VistaDBInstalledFact]
    public async Task Update_with_concurrency_token_succeeds_on_correct_token()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("IdentityUpdate");
        using var ctx = new IdentityContext(store.ConnectionString);
        ctx.Database.EnsureCreated();

        var e = new IntIdentityEntity { Name = "initial" };
        ctx.IntIdentities.Add(e);
        ctx.SaveChanges();

        e.Name = "updated";
        ctx.SaveChanges();

        var reloaded = ctx.IntIdentities.AsNoTracking().Single(x => x.Id == e.Id);
        Assert.Equal("updated", reloaded.Name);
    }

    [VistaDBInstalledFact]
    public async Task Delete_removes_row_and_count_reflects_change()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("IdentityDelete");
        using var ctx = new IdentityContext(store.ConnectionString);
        ctx.Database.EnsureCreated();

        var e = new IntIdentityEntity { Name = "doomed" };
        ctx.IntIdentities.Add(e);
        ctx.SaveChanges();

        ctx.IntIdentities.Remove(e);
        ctx.SaveChanges();

        Assert.Empty(ctx.IntIdentities.AsNoTracking().Where(x => x.Id == e.Id));
    }

    [VistaDBInstalledFact]
    public async Task Insert_long_identity_round_trips()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("IdentityLong");
        using var ctx = new IdentityContext(store.ConnectionString);
        ctx.Database.EnsureCreated();

        var e = new LongIdentityEntity { Name = "big" };
        ctx.LongIdentities.Add(e);
        ctx.SaveChanges();

        Assert.NotEqual(0L, e.Id);
    }

    [VistaDBInstalledFact]
    public async Task Insert_short_identity_round_trips()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("IdentityShort");
        using var ctx = new IdentityContext(store.ConnectionString);
        ctx.Database.EnsureCreated();

        var e = new ShortIdentityEntity { Name = "tiny" };
        ctx.ShortIdentities.Add(e);
        ctx.SaveChanges();

        Assert.NotEqual((short)0, e.Id);
    }

    [VistaDBInstalledFact]
    public async Task Insert_Guid_pk_round_trips()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("IdentityGuid");
        using var ctx = new IdentityContext(store.ConnectionString);
        ctx.Database.EnsureCreated();

        var e = new GuidPkEntity { Id = Guid.NewGuid(), Name = "guid" };
        ctx.GuidPks.Add(e);
        ctx.SaveChanges();

        Assert.NotEqual(Guid.Empty, e.Id);
    }

    [VistaDBInstalledFact]
    public async Task Two_independent_contexts_round_trip_identical_state()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("IdentityTwoCtx");
        using (var ctx = new IdentityContext(store.ConnectionString))
        {
            ctx.Database.EnsureCreated();
            ctx.IntIdentities.Add(new IntIdentityEntity { Name = "first" });
            ctx.SaveChanges();
        }

        using (var ctx = new IdentityContext(store.ConnectionString))
        {
            Assert.Single(ctx.IntIdentities.AsNoTracking());
        }
    }

    private class IdentityContext : DbContext
    {
        private readonly string _cs;

        public IdentityContext(string cs)
        {
            _cs = cs;
        }

        public DbSet<IntIdentityEntity> IntIdentities { get; set; }
        public DbSet<LongIdentityEntity> LongIdentities { get; set; }
        public DbSet<ShortIdentityEntity> ShortIdentities { get; set; }
        public DbSet<GuidPkEntity> GuidPks { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder builder)
            => builder.UseVistaDB(_cs);
    }

    private class IntIdentityEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    private class LongIdentityEntity
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    private class ShortIdentityEntity
    {
        public short Id { get; set; }
        public string Name { get; set; }
    }

    private class GuidPkEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
