// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.TestUtilities;

/// <summary>
///     Lightweight test-only fixture that exposes a shared, validated VistaDB internal service
///     provider. The parallel <c>SqlServerFixture.DefaultServiceProvider</c> lives in
///     <c>EFCore.SqlServer.FunctionalTests</c>; we keep the VistaDB analog inside the unit-test
///     project itself to avoid a hard dependency from unit tests onto the functional-tests
///     fixture.
/// </summary>
public static class VistaDBFixture
{
    public static IServiceProvider DefaultServiceProvider { get; }
        = new ServiceCollection().AddEntityFrameworkVistaDB().BuildServiceProvider(validateScopes: true);
}
