// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

#nullable disable

public class VistaDBFixture : ServiceProviderFixtureBase
{
    public static IServiceProvider DefaultServiceProvider { get; }
        = new ServiceCollection().AddEntityFrameworkVistaDB().BuildServiceProvider(validateScopes: true);

    public TestSqlLoggerFactory TestSqlLoggerFactory
        => (TestSqlLoggerFactory)ServiceProvider.GetRequiredService<ILoggerFactory>();

    protected override ITestStoreFactory TestStoreFactory
        => VistaDBTestStoreFactory.Instance;
}
