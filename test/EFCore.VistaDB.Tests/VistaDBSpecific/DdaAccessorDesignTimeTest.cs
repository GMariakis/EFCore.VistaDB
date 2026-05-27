// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Pure design-time unit tests for <see cref="VistaDBDdaAccessor"/>. No live VistaDB engine is
///     required — these exercise the dispose lifecycle, lazy-resolution flag, and constructor behavior.
/// </summary>
public class DdaAccessorDesignTimeTest
{
    [ConditionalFact]
    public void Constructor_with_null_connection_does_not_throw()
    {
        using var a = new VistaDBDdaAccessor(connection: null);
        Assert.NotNull(a);
    }

    [ConditionalFact]
    public void IsResolved_is_false_immediately_after_construction()
    {
        using var a = new VistaDBDdaAccessor(connection: null);
        Assert.False(a.IsResolved);
    }

    [ConditionalFact]
    public void IsResolved_is_false_after_dispose()
    {
        var a = new VistaDBDdaAccessor(connection: null);
        a.Dispose();
        Assert.False(a.IsResolved);
    }

    [ConditionalFact]
    public void Dispose_is_idempotent_when_called_twice()
    {
        var a = new VistaDBDdaAccessor(connection: null);
        a.Dispose();
        a.Dispose();
        Assert.False(a.IsResolved);
    }

    [ConditionalFact]
    public void Dispose_is_idempotent_when_called_three_times()
    {
        var a = new VistaDBDdaAccessor(connection: null);
        a.Dispose();
        a.Dispose();
        a.Dispose();
        Assert.False(a.IsResolved);
    }

    [ConditionalFact]
    public void Multiple_independent_accessors_have_independent_lifecycles()
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

    [ConditionalFact]
    public void Accessor_implements_IDisposable()
    {
        var a = new VistaDBDdaAccessor(connection: null);
        Assert.IsAssignableFrom<IDisposable>(a);
        a.Dispose();
    }

    [ConditionalFact]
    public void Accessor_implements_IVistaDBDdaAccessor_interface()
    {
        using var a = new VistaDBDdaAccessor(connection: null);
        Assert.IsAssignableFrom<IVistaDBDdaAccessor>(a);
    }
}
