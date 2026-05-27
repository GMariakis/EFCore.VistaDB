// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Metadata.Conventions;

namespace Microsoft.EntityFrameworkCore.TestUtilities;

/// <summary>
///     Standalone test helper. Mirrors the public surface of the in-tree
///     <c>RelationalTestHelpers</c>-derived <c>VistaDBTestHelpers</c> without depending on
///     <c>EFCore.Relational.Tests</c> or <c>EFCore.TestUtilities</c>.
/// </summary>
public class VistaDBTestHelpers
{
    protected VistaDBTestHelpers() { }

    public static VistaDBTestHelpers Instance { get; } = new();

    /// <summary>Creates a <see cref="ModelBuilder"/> with the full VistaDB convention set applied.</summary>
    public ModelBuilder CreateConventionBuilder()
    {
        // Mirror what RelationalTestHelpers.CreateConventionBuilder() does in-tree:
        // build a scoped service provider from a lightweight DbContext (no real DB connection
        // is opened — GetInfrastructure() is safe against a dummy path), then resolve the
        // convention set builder from that provider.
        var options = new DbContextOptionsBuilder()
            .UseVistaDB("Data Source=DummyDatabase.vdb6")
            .EnableServiceProviderCaching(false)
            .Options;

        using var ctx = new DbContext(options);
        var conventionSetBuilder = ctx.GetInfrastructure()
            .GetRequiredService<IConventionSetBuilder>();
        return new ModelBuilder(conventionSetBuilder.CreateConventionSet());
    }

    /// <summary>
    ///     Creates a scoped <see cref="IServiceProvider"/> configured for VistaDB and
    ///     bound to the supplied <paramref name="model"/>. Useful for resolving provider
    ///     services (e.g. value-generator caches) in unit tests that do not open a real database.
    ///     The returned provider is backed by an undisposed <see cref="DbContext"/> so that the
    ///     scope stays live for the duration of the test (the context is GC-collected eventually).
    /// </summary>
    public IServiceProvider CreateContextServices(IModel model)
    {
        var options = new DbContextOptionsBuilder()
            .UseVistaDB("Data Source=DummyDatabase.vdb6")
            .UseModel(model)
            .EnableServiceProviderCaching(false)
            .Options;

        // NOTE: intentionally no 'using' — the scope must stay alive so callers can resolve
        // services from the returned IServiceProvider after this method returns.
        var ctx = new DbContext(options);
        return ctx.GetInfrastructure();
    }
}
