// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;

// ReSharper disable UnusedMember.Local
namespace Microsoft.EntityFrameworkCore.ValueGeneration;

public class VistaDBValueGeneratorSelectorTest
{
    [ConditionalTheory, InlineData(true), InlineData(false)]
    public void Returns_built_in_generators_for_types_setup_for_value_generation(bool useTry)
    {
        AssertGenerator<TemporaryIntValueGenerator>("Id", useTry: useTry);
        AssertGenerator<CustomValueGenerator>("Custom", useTry: useTry);
        AssertGenerator<TemporaryLongValueGenerator>("Long", useTry: useTry);
        AssertGenerator<TemporaryShortValueGenerator>("Short", useTry: useTry);
        AssertGenerator<TemporaryByteValueGenerator>("Byte", useTry: useTry);
        AssertGenerator<TemporaryIntValueGenerator>("NullableInt", useTry: useTry);
        AssertGenerator<TemporaryLongValueGenerator>("NullableLong", useTry: useTry);
        AssertGenerator<TemporaryShortValueGenerator>("NullableShort", useTry: useTry);
        AssertGenerator<TemporaryByteValueGenerator>("NullableByte", useTry: useTry);
        AssertGenerator<TemporaryDecimalValueGenerator>("Decimal", useTry: useTry);
        AssertGenerator<StringValueGenerator>("String", useTry: useTry);
        AssertGenerator<SequentialGuidValueGenerator>("Guid", useTry: useTry);
        AssertGenerator<BinaryValueGenerator>("Binary", useTry: useTry);
    }

    private void AssertGenerator<TExpected>(string propertyName, bool useTry = true)
    {
        var builder = VistaDBTestHelpers.Instance.CreateConventionBuilder();
        builder.Entity<AnEntity>(b =>
        {
            b.Property(e => e.Custom).HasValueGenerator<CustomValueGenerator>();
            b.Property(propertyName).ValueGeneratedOnAdd();
            b.HasKey(propertyName);
        });

        var model = builder.FinalizeModel();
        var entityType = model.FindEntityType(typeof(AnEntity))!;

        var selector = VistaDBTestHelpers.Instance.CreateContextServices(model).GetRequiredService<IValueGeneratorSelector>();

        var property = entityType.FindProperty(propertyName)!;
        var generator = CreateValueGenerator(selector, property, useTry);

        Assert.IsType<TExpected>(generator);
    }

    private static ValueGenerator CreateValueGenerator(IValueGeneratorSelector selector, IProperty property, bool useTry)
    {
        ValueGenerator generator;
        if (useTry)
        {
            selector.TrySelect(property, property.DeclaringType, out generator);
        }
        else
        {
#pragma warning disable CS0618 // Type or member is obsolete
            generator = selector.Select(property, property.DeclaringType);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        return generator;
    }

    [ConditionalTheory, InlineData(true), InlineData(false)]
    public void Returns_temp_guid_generator_when_default_sql_set(bool useTry)
    {
        var builder = VistaDBTestHelpers.Instance.CreateConventionBuilder();
        builder.Entity<AnEntity>(b =>
        {
            b.Property(e => e.Guid).HasDefaultValueSql("newid()");
            b.HasKey(e => e.Guid);
        });
        var model = builder.FinalizeModel();
        var entityType = model.FindEntityType(typeof(AnEntity))!;

        var selector = VistaDBTestHelpers.Instance.CreateContextServices(model).GetRequiredService<IValueGeneratorSelector>();

        var property = entityType.FindProperty("Guid")!;
        var generator = CreateValueGenerator(selector, property, useTry);
        Assert.IsType<TemporaryGuidValueGenerator>(generator);
    }

    [ConditionalTheory, InlineData(true), InlineData(false)]
    public void Returns_temp_string_generator_when_default_sql_set(bool useTry)
    {
        var builder = VistaDBTestHelpers.Instance.CreateConventionBuilder();
        builder.Entity<AnEntity>(b =>
        {
            b.Property(e => e.String).ValueGeneratedOnAdd().HasDefaultValueSql("Foo");
            b.HasKey(e => e.String);
        });
        var model = builder.FinalizeModel();
        var entityType = model.FindEntityType(typeof(AnEntity))!;

        var selector = VistaDBTestHelpers.Instance.CreateContextServices(model).GetRequiredService<IValueGeneratorSelector>();

        var property = entityType.FindProperty("String")!;
        var generator = CreateValueGenerator(selector, property, useTry);

        Assert.IsType<TemporaryStringValueGenerator>(generator);
        Assert.True(generator.GeneratesTemporaryValues);
    }

    [ConditionalTheory, InlineData(true), InlineData(false)]
    public void Returns_temp_binary_generator_when_default_sql_set(bool useTry)
    {
        var builder = VistaDBTestHelpers.Instance.CreateConventionBuilder();
        builder.Entity<AnEntity>(b =>
        {
            b.HasKey(e => e.Binary);
            b.Property(e => e.Binary).HasDefaultValueSql("Foo").ValueGeneratedOnAdd();
        });
        var model = builder.FinalizeModel();
        var entityType = model.FindEntityType(typeof(AnEntity))!;

        var selector = VistaDBTestHelpers.Instance.CreateContextServices(model).GetRequiredService<IValueGeneratorSelector>();

        var property = entityType.FindProperty("Binary")!;
        var generator = CreateValueGenerator(selector, property, useTry);

        Assert.IsType<TemporaryBinaryValueGenerator>(generator);
        Assert.True(generator.GeneratesTemporaryValues);
    }

    // VistaDB: no analog — VistaDB has no HiLo/Sequence value generators.
    // Original SqlServer tests preserved below for future revival when VistaDB adds support.
    /*
    [ConditionalTheory, InlineData(true), InlineData(false)]
    public void Returns_sequence_value_generators_when_configured_for_model(bool useTry) { ... }

    [ConditionalTheory, InlineData(true), InlineData(false)]
    public void Returns_built_in_generators_for_types_setup_for_value_generation_even_with_key_sequences(bool useTry) { ... }

    [ConditionalTheory, InlineData(true), InlineData(false)]
    public void Returns_generator_configured_on_model_when_property_is_identity(bool useTry)
    {
        // Uses SqlServerSequenceHiLoValueGenerator<int>.
    }
    */

    [ConditionalFact]
    public void Throws_for_unsupported_combinations()
    {
        // Use VistaDBTestHelpers (same as InMemoryTestHelpers for basic model building)
        var builder = VistaDBTestHelpers.Instance.CreateConventionBuilder();
        builder.Entity<AnEntity>(b =>
        {
            b.Property(e => e.TimeSpan).ValueGeneratedOnAdd();
            b.HasKey(e => e.TimeSpan);
        });
        var model = builder.FinalizeModel();
        var entityType = model.FindEntityType(typeof(AnEntity));

        var selector = VistaDBTestHelpers.Instance.CreateContextServices(model).GetRequiredService<IValueGeneratorSelector>();

        Assert.Equal(
            CoreStrings.NoValueGenerator("TimeSpan", "AnEntity", "TimeSpan"),
            Assert.Throws<NotSupportedException>((Action)(() => selector.Select(entityType.FindProperty("TimeSpan"), entityType))).Message);
    }

    private class AnEntity
    {
        public int Id { get; set; }
        public int Custom { get; set; }
        public long Long { get; set; }
        public short Short { get; set; }
        public byte Byte { get; set; }
        public int? NullableInt { get; set; }
        public long? NullableLong { get; set; }
        public short? NullableShort { get; set; }
        public byte? NullableByte { get; set; }
        public string String { get; set; }
        public Guid Guid { get; set; }
        public byte[] Binary { get; set; }
        public float Float { get; set; }
        public decimal Decimal { get; set; }
        public TimeSpan TimeSpan { get; set; }

        [NotMapped]
        public Something Random { get; set; }
    }

    private struct Something : IComparable<Something>
    {
        public int Id { get; set; }

        public int CompareTo(Something other)
            => throw new NotImplementedException();
    }

    private class CustomValueGenerator : ValueGenerator<int>
    {
        public override int Next(EntityEntry entry)
            => throw new NotImplementedException();

        public override bool GeneratesTemporaryValues
            => false;
    }
}
