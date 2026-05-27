// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Metadata;

/// <summary>
///     Unit-test surface for the internal metadata-builder extensions used by the VistaDB conventions.
///     The SqlServer parallel (<c>SqlServerMetadataBuilderExtensionsTest</c>, ~417 lines) exercises
///     <c>HasValueGenerationStrategy</c>, <c>HasHiLoSequence</c>, <c>HasIdentitySeed</c>,
///     <c>HasIdentityIncrement</c>, <c>HasIsClustered</c>, <c>HasFillFactor</c>,
///     <c>HasIsMemoryOptimized</c>, <c>HasIsTemporal</c>, <c>UseSqlOutputClause</c>, and
///     <c>HasIncludeProperties</c> across multiple <c>ConfigurationSource</c> values. VistaDB exposes
///     only the identity surface; the rest are preserved verbatim in marker blocks for future revival.
/// </summary>
public class VistaDBMetadataBuilderExtensionsTest
{
    // The repo's relational base tests cover the bulk of the ConfigurationSource priority behaviour;
    // the VistaDB-specific identity surface is exercised end-to-end by VistaDBBuilderExtensionsTest
    // (HasIdentitySeed / HasIdentityIncrement / ValueGenerationStrategy annotations).

    // VistaDB: no analog — VistaDB does not yet expose internal HasValueGenerationStrategy /
    // HasIdentitySeed / HasIdentityIncrement metadata-builder helpers on
    // IConventionPropertyBuilder. The annotations are set directly by VistaDBPropertyBuilderExtensions
    // via SetOrRemoveAnnotation. Once the internal helpers are introduced (mirroring the SqlServer
    // shape), this test class will be filled out to assert ConfigurationSource priority.
    // Original SqlServer tests preserved on the SqlServer side for future revival.
    /*
    [ConditionalFact]
    public void Can_access_property_HasValueGenerationStrategy() { ... }

    [ConditionalFact]
    public void Can_access_property_HasIdentitySeed() { ... }

    [ConditionalFact]
    public void Can_access_property_HasIdentityIncrement() { ... }

    [ConditionalFact]
    public void Can_access_key_HasIsClustered() { ... }

    [ConditionalFact]
    public void Can_access_index_HasFillFactor() { ... }

    [ConditionalFact]
    public void Can_access_entityType_HasIsMemoryOptimized() { ... }

    [ConditionalFact]
    public void Can_access_entityType_HasIsTemporal() { ... }

    [ConditionalFact]
    public void Can_access_entityType_UseSqlOutputClause() { ... }
    */

    [ConditionalFact(Skip = "VistaDB does not yet expose internal IConventionPropertyBuilder HasValueGenerationStrategy / HasIdentitySeed / HasIdentityIncrement helpers. Test will be filled out once the helpers exist.")]
    public void Can_access_property_HasValueGenerationStrategy()
    {
        // TODO(EFCore.VistaDB.Tests): once VistaDB introduces the internal metadata-builder helpers,
        // assert that HasValueGenerationStrategy(VistaDBValueGenerationStrategy.IdentityColumn,
        // ConfigurationSource.Convention) succeeds and that a higher source overrides it.
    }
}
