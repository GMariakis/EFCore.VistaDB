// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     VistaDB Tier-2 DDA hook behavior. Pure design-time properties are exercised with
///     <see cref="ConditionalFactAttribute"/>; the live-engine paths use
///     <see cref="VistaDBInstalledFactAttribute"/> so CI without the native bits skips cleanly.
/// </summary>
public class DdaAccessorBehaviorTest
{
    [ConditionalFact]
    public void IsResolved_is_false_before_first_DDA_access()
    {
        var accessor = new VistaDBDdaAccessor(connection: null);
        Assert.False(accessor.IsResolved);
    }

    [ConditionalFact]
    public void Dispose_is_idempotent_when_DDA_was_never_resolved()
    {
        var accessor = new VistaDBDdaAccessor(connection: null);
        accessor.Dispose();
        accessor.Dispose();
        Assert.False(accessor.IsResolved);
    }

    [ConditionalFact]
    public void IsResolved_is_false_after_dispose()
    {
        var accessor = new VistaDBDdaAccessor(connection: null);
        accessor.Dispose();
        Assert.False(accessor.IsResolved);
    }

    [ConditionalFact]
    public void Accessor_can_be_constructed_with_null_connection_without_throwing()
    {
        // VistaDB: lazy resolution — constructor must NOT touch the connection.
        var accessor = new VistaDBDdaAccessor(connection: null);
        Assert.NotNull(accessor);
        accessor.Dispose();
    }

    [ConditionalFact]
    public void Multiple_independent_accessors_do_not_interfere()
    {
        var a = new VistaDBDdaAccessor(connection: null);
        var b = new VistaDBDdaAccessor(connection: null);
        Assert.False(a.IsResolved);
        Assert.False(b.IsResolved);
        a.Dispose();
        Assert.False(a.IsResolved);
        Assert.False(b.IsResolved);
        b.Dispose();
    }

    [VistaDBInstalledFact]
    public void Dda_property_lazily_opens_DDA_when_engine_is_available()
    {
        using var accessor = CreateAccessorFromDI("Data Source=test_dda_lazy.vdb6");
        Assert.False(accessor.IsResolved);
        Assert.NotNull(accessor.Dda);
        Assert.True(accessor.IsResolved);
    }

    [VistaDBInstalledFact]
    public void Accessor_resolved_via_DI_round_trip_through_DbContext()
    {
        using var accessor = CreateAccessorFromDI("Data Source=test_dda_di.vdb6");
        Assert.NotNull(accessor);
    }

    private static IVistaDBDdaAccessor CreateAccessorFromDI(string connectionString)
    {
        var sp = new ServiceCollection()
            .AddEntityFrameworkVistaDB()
            .AddDbContext<TestContext>(
                (s, b) => b.UseInternalServiceProvider(s).UseVistaDB(connectionString))
            .BuildServiceProvider();

        var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TestContext>();
        return ctx.GetService<IVistaDBDdaAccessor>();
    }

    private class TestContext : DbContext
    {
        public TestContext(DbContextOptions<TestContext> options)
            : base(options)
        {
        }
    }
}
