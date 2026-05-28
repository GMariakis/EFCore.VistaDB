// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Diagnostics.Internal;

// ReSharper disable InconsistentNaming
namespace Microsoft.EntityFrameworkCore.Diagnostics;

public class VistaDBEventIdTest : EventIdTestBase
{
    [ConditionalFact(Skip = "VistaDB provider does not yet define a public VistaDBEventId / VistaDBLoggerExtensions surface — only VistaDBLoggingDefinitions exists. Test will be enabled once the public diagnostic surface is added.")]
    public void Every_eventId_has_a_logger_method_and_logs_when_level_enabled()
    {
        // VistaDB: no analog — VistaDB does not yet expose a VistaDBEventId enum or
        // VistaDBLoggerExtensions class. The provider currently relies entirely on the
        // relational base events. Once provider-specific diagnostic events are introduced
        // (e.g., for DDA fallback paths), this test should mirror SqlServerEventIdTest.
        // Original SqlServer test preserved below for future revival when VistaDB adds support.
        /*
        var entityType = new EntityType(typeof(object), new Model(new ConventionSet()), owned: false, ConfigurationSource.Convention);
        var property = new Property(
            "A", typeof(int), null, null, entityType, ConfigurationSource.Convention, ConfigurationSource.Convention);
        entityType.Model.FinalizeModel();

        var fakeFactories = new Dictionary<Type, Func<object>>
        {
            { typeof(IList<string>), () => new List<string> { "Fake1", "Fake2" } },
            { typeof(IProperty), () => property },
            { typeof(IEntityType), () => entityType },
            { typeof(IReadOnlyProperty), () => property },
            { typeof(string), () => "Fake" }
        };

        TestEventLogging(
            typeof(VistaDBEventId),
            typeof(VistaDBLoggerExtensions),
            new VistaDBLoggingDefinitions(),
            fakeFactories);
        */
    }

    [ConditionalFact]
    public void VistaDBLoggingDefinitions_can_be_instantiated()
    {
        // Lightweight smoke test until a proper VistaDBEventId enum exists.
        var definitions = new VistaDBLoggingDefinitions();
        Assert.NotNull(definitions);
    }
}
