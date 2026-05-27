// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.TestUtilities;

/// <summary>
///     Standalone stub for EF Core's in-tree <c>TestServiceFactory</c>.
///     Builds a minimal DI container via <see cref="ServiceCollection"/> so tests that call
///     <c>TestServiceFactory.Instance.Create&lt;T&gt;()</c> receive properly registered EF Core
///     dependencies (e.g. <see cref="TypeMappingSourceDependencies"/>).
/// </summary>
public sealed class TestServiceFactory
{
    public static TestServiceFactory Instance { get; } = new();

    private readonly IServiceProvider _services;

    private TestServiceFactory()
    {
        _services = new ServiceCollection()
            .AddEntityFrameworkVistaDB()
            .BuildServiceProvider(validateScopes: false);
    }

    public T Create<T>() where T : class
        => _services.GetRequiredService<T>();
}
