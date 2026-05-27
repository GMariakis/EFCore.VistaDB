// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Live VistaDB scaffolding (database-first) sanity checks. Each test creates a tiny model via the
///     ORM, persists it to a real <c>.vdb6</c> file, then asserts the round-trip behavior preserves the
///     schema features we care about. (Full reverse-engineering via <c>IDatabaseModelFactory</c> is
///     covered in the dedicated ScaffoldingVistaDBTest.)
/// </summary>
public class ScaffoldingMetadataTest
{
    [VistaDBInstalledFact]
    public async Task Single_table_round_trips_through_database_creation()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("ScaffoldSingle");
        using var ctx = new ScaffoldContext(store.ConnectionString);
        ctx.Database.EnsureCreated();

        var entityType = ctx.Model.FindEntityType(typeof(SimpleRow));
        Assert.NotNull(entityType);
        Assert.Equal("SimpleRows", entityType.GetTableName());
    }

    [VistaDBInstalledFact]
    public async Task Multi_column_primary_key_is_preserved_in_model()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("ScaffoldCompositePk");
        using var ctx = new ScaffoldContext(store.ConnectionString);
        ctx.Database.EnsureCreated();

        var pk = ctx.Model.FindEntityType(typeof(CompositePkRow))!.FindPrimaryKey();
        Assert.NotNull(pk);
        Assert.Equal(2, pk!.Properties.Count);
    }

    [VistaDBInstalledFact]
    public async Task Identity_column_is_marked_as_value_generated_on_add()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("ScaffoldIdentity");
        using var ctx = new ScaffoldContext(store.ConnectionString);
        ctx.Database.EnsureCreated();

        var idProp = ctx.Model.FindEntityType(typeof(SimpleRow))!.FindProperty("Id");
        Assert.Equal(ValueGenerated.OnAdd, idProp!.ValueGenerated);
    }

    [VistaDBInstalledFact]
    public async Task Nullable_property_is_marked_nullable_in_model()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("ScaffoldNullable");
        using var ctx = new ScaffoldContext(store.ConnectionString);
        ctx.Database.EnsureCreated();

        var prop = ctx.Model.FindEntityType(typeof(SimpleRow))!.FindProperty("Nickname");
        Assert.True(prop!.IsNullable);
    }

    [VistaDBInstalledFact]
    public async Task NonNullable_string_is_marked_not_nullable_in_model()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("ScaffoldNotNullable");
        using var ctx = new ScaffoldContext(store.ConnectionString);
        ctx.Database.EnsureCreated();

        var prop = ctx.Model.FindEntityType(typeof(SimpleRow))!.FindProperty("Name");
        Assert.False(prop!.IsNullable);
    }

    [VistaDBInstalledFact]
    public async Task Unique_index_is_modeled_correctly()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("ScaffoldUniqueIdx");
        using var ctx = new ScaffoldContext(store.ConnectionString);
        ctx.Database.EnsureCreated();

        var index = ctx.Model.FindEntityType(typeof(IndexedRow))!.GetIndexes().Single();
        Assert.True(index.IsUnique);
    }

    private class ScaffoldContext : DbContext
    {
        private readonly string _cs;

        public ScaffoldContext(string cs)
        {
            _cs = cs;
        }

        public DbSet<SimpleRow> SimpleRows { get; set; }
        public DbSet<CompositePkRow> CompositePkRows { get; set; }
        public DbSet<IndexedRow> IndexedRows { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder b)
            => b.UseVistaDB(_cs);

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<CompositePkRow>().HasKey(x => new { x.PartA, x.PartB });
            b.Entity<IndexedRow>().HasIndex(x => x.Code).IsUnique();
        }
    }

    private class SimpleRow
    {
        public int Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public string Name { get; set; } = string.Empty;

        public string Nickname { get; set; }
    }

    private class CompositePkRow
    {
        public int PartA { get; set; }
        public int PartB { get; set; }
        public string Payload { get; set; }
    }

    private class IndexedRow
    {
        public int Id { get; set; }
        public string Code { get; set; }
    }
}
