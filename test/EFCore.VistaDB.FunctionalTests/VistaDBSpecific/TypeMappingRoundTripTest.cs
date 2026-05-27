// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Round-trips each supported CLR type through a live VistaDB store and asserts the value survives
///     intact. SqlServer-only types (Vector, StructuralJson, UDT, SqlVariant) are deliberately omitted —
///     their rejection is verified in <see cref="UnsupportedFeatureErrorMessagesTest"/>.
/// </summary>
public class TypeMappingRoundTripTest
{
    [VistaDBInstalledFact]
    public async Task Bool_round_trips()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("TypeBool");
        using var ctx = new TypesContext(store.ConnectionString);
        ctx.Database.EnsureCreated();
        ctx.Items.Add(new TypeBag { Id = 1, BoolValue = true });
        ctx.SaveChanges();
        Assert.True(ctx.Items.AsNoTracking().Single().BoolValue);
    }

    [VistaDBInstalledFact]
    public async Task Byte_round_trips()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("TypeByte");
        using var ctx = new TypesContext(store.ConnectionString);
        ctx.Database.EnsureCreated();
        ctx.Items.Add(new TypeBag { Id = 1, ByteValue = 42 });
        ctx.SaveChanges();
        Assert.Equal((byte)42, ctx.Items.AsNoTracking().Single().ByteValue);
    }

    [VistaDBInstalledFact]
    public async Task Short_round_trips()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("TypeShort");
        using var ctx = new TypesContext(store.ConnectionString);
        ctx.Database.EnsureCreated();
        ctx.Items.Add(new TypeBag { Id = 1, ShortValue = 1234 });
        ctx.SaveChanges();
        Assert.Equal((short)1234, ctx.Items.AsNoTracking().Single().ShortValue);
    }

    [VistaDBInstalledFact]
    public async Task Int_round_trips()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("TypeInt");
        using var ctx = new TypesContext(store.ConnectionString);
        ctx.Database.EnsureCreated();
        ctx.Items.Add(new TypeBag { Id = 1, IntValue = 999_999 });
        ctx.SaveChanges();
        Assert.Equal(999_999, ctx.Items.AsNoTracking().Single().IntValue);
    }

    [VistaDBInstalledFact]
    public async Task Long_round_trips()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("TypeLong");
        using var ctx = new TypesContext(store.ConnectionString);
        ctx.Database.EnsureCreated();
        ctx.Items.Add(new TypeBag { Id = 1, LongValue = 9_000_000_000L });
        ctx.SaveChanges();
        Assert.Equal(9_000_000_000L, ctx.Items.AsNoTracking().Single().LongValue);
    }

    [VistaDBInstalledFact]
    public async Task Decimal_round_trips()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("TypeDecimal");
        using var ctx = new TypesContext(store.ConnectionString);
        ctx.Database.EnsureCreated();
        ctx.Items.Add(new TypeBag { Id = 1, DecimalValue = 1234.56m });
        ctx.SaveChanges();
        Assert.Equal(1234.56m, ctx.Items.AsNoTracking().Single().DecimalValue);
    }

    [VistaDBInstalledFact]
    public async Task Float_round_trips()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("TypeFloat");
        using var ctx = new TypesContext(store.ConnectionString);
        ctx.Database.EnsureCreated();
        ctx.Items.Add(new TypeBag { Id = 1, FloatValue = 1.5f });
        ctx.SaveChanges();
        Assert.Equal(1.5f, ctx.Items.AsNoTracking().Single().FloatValue);
    }

    [VistaDBInstalledFact]
    public async Task Double_round_trips()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("TypeDouble");
        using var ctx = new TypesContext(store.ConnectionString);
        ctx.Database.EnsureCreated();
        ctx.Items.Add(new TypeBag { Id = 1, DoubleValue = 3.14159 });
        ctx.SaveChanges();
        Assert.Equal(3.14159, ctx.Items.AsNoTracking().Single().DoubleValue);
    }

    [VistaDBInstalledFact]
    public async Task String_round_trips()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("TypeString");
        using var ctx = new TypesContext(store.ConnectionString);
        ctx.Database.EnsureCreated();
        ctx.Items.Add(new TypeBag { Id = 1, StringValue = "Hello, VistaDB!" });
        ctx.SaveChanges();
        Assert.Equal("Hello, VistaDB!", ctx.Items.AsNoTracking().Single().StringValue);
    }

    [VistaDBInstalledFact]
    public async Task ByteArray_round_trips()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("TypeByteArray");
        using var ctx = new TypesContext(store.ConnectionString);
        ctx.Database.EnsureCreated();
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        ctx.Items.Add(new TypeBag { Id = 1, BytesValue = bytes });
        ctx.SaveChanges();
        Assert.Equal(bytes, ctx.Items.AsNoTracking().Single().BytesValue);
    }

    [VistaDBInstalledFact]
    public async Task Guid_round_trips()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("TypeGuid");
        using var ctx = new TypesContext(store.ConnectionString);
        ctx.Database.EnsureCreated();
        var g = Guid.NewGuid();
        ctx.Items.Add(new TypeBag { Id = 1, GuidValue = g });
        ctx.SaveChanges();
        Assert.Equal(g, ctx.Items.AsNoTracking().Single().GuidValue);
    }

    [VistaDBInstalledFact]
    public async Task DateTime_round_trips()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("TypeDateTime");
        using var ctx = new TypesContext(store.ConnectionString);
        ctx.Database.EnsureCreated();
        var dt = new DateTime(2024, 5, 22, 10, 30, 0);
        ctx.Items.Add(new TypeBag { Id = 1, DateTimeValue = dt });
        ctx.SaveChanges();
        Assert.Equal(dt, ctx.Items.AsNoTracking().Single().DateTimeValue);
    }

    [VistaDBInstalledFact]
    public async Task DateOnly_round_trips()
    {
        await using var store = await VistaDBTestStore.CreateInitializedAsync("TypeDateOnly");
        using var ctx = new TypesContext(store.ConnectionString);
        ctx.Database.EnsureCreated();
        var d = new DateOnly(2024, 5, 22);
        ctx.Items.Add(new TypeBag { Id = 1, DateOnlyValue = d });
        ctx.SaveChanges();
        Assert.Equal(d, ctx.Items.AsNoTracking().Single().DateOnlyValue);
    }

    private class TypesContext : DbContext
    {
        private readonly string _cs;

        public TypesContext(string cs)
        {
            _cs = cs;
        }

        public DbSet<TypeBag> Items { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder b)
            => b.UseVistaDB(_cs);
    }

    private class TypeBag
    {
        public int Id { get; set; }
        public bool BoolValue { get; set; }
        public byte ByteValue { get; set; }
        public short ShortValue { get; set; }
        public int IntValue { get; set; }
        public long LongValue { get; set; }
        public decimal DecimalValue { get; set; }
        public float FloatValue { get; set; }
        public double DoubleValue { get; set; }
        public string StringValue { get; set; }
        public byte[] BytesValue { get; set; }
        public Guid GuidValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public DateOnly DateOnlyValue { get; set; }
    }
}
