// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.ValueGeneration.Internal;

// ReSharper disable InconsistentNaming
namespace Microsoft.EntityFrameworkCore.ValueGeneration;

public class VistaDBValueGeneratorCacheTest
{
    [ConditionalFact]
    public void Uses_single_generator_per_property()
    {
        var model = CreateModel();
        var entityType = model.FindEntityType(typeof(Led));
        var property1 = GetProperty1(model);
        var property2 = GetProperty2(model);
        var cache = VistaDBTestHelpers.Instance.CreateContextServices(model).GetRequiredService<IVistaDBValueGeneratorCache>();

        var generator1 = cache.GetOrAdd(property1, entityType, (p, et) => new TemporaryIntValueGenerator());
        Assert.NotNull(generator1);
        Assert.Same(generator1, cache.GetOrAdd(property1, entityType, (p, et) => new TemporaryIntValueGenerator()));

        var generator2 = cache.GetOrAdd(property2, entityType, (p, et) => new TemporaryIntValueGenerator());
        Assert.NotNull(generator2);
        Assert.Same(generator2, cache.GetOrAdd(property2, entityType, (p, et) => new TemporaryIntValueGenerator()));
        Assert.NotSame(generator1, generator2);
    }

    // VistaDB: no analog — VistaDB has no CREATE SEQUENCE, so HiLo and Sequence value generators do not apply.
    // The entire SqlServer "Uses_single_sequence_generator_per_*", "Block_size_*", "Sequence_name_*" and
    // "Schema_qualified_sequence_name_*" suite is omitted. The IVistaDBValueGeneratorCache interface also
    // does not expose GetOrAddSequenceState — see Storage/Internal/IVistaDBValueGeneratorCache.cs.
    // Original SqlServer tests preserved below for future revival when VistaDB adds support.
    /*
    [ConditionalFact]
    public void Uses_single_sequence_generator_per_sequence() { ... }
    [ConditionalFact]
    public void Block_size_is_obtained_from_default_sequence() { ... }
    [ConditionalFact]
    public void Sequence_name_is_obtained_from_default_sequence() { ... }
    [ConditionalFact]
    public void Schema_qualified_sequence_name_is_obtained_from_named_sequence() { ... }
    */

    protected virtual ModelBuilder CreateConventionModelBuilder()
        => VistaDBTestHelpers.Instance.CreateConventionBuilder();

    private static IProperty GetProperty1(IModel model)
        => model.FindEntityType(typeof(Led)).FindProperty("Zeppelin");

    private static IProperty GetProperty2(IModel model)
        => model.FindEntityType(typeof(Led)).FindProperty("Stairway");

    private static IModel CreateModel()
    {
        var modelBuilder = VistaDBTestHelpers.Instance.CreateConventionBuilder();

        modelBuilder.Entity<Led>(b =>
        {
            b.Property<int>("Id");
            b.Property(e => e.Zeppelin);
            b.HasAlternateKey(e => e.Zeppelin);
            b.Property(e => e.Stairway);
            b.HasAlternateKey(e => e.Stairway);
        });

        return modelBuilder.Model.FinalizeModel();
    }

    private class Led
    {
        public int Zeppelin { get; set; }
        public int Stairway { get; set; }
    }
}
