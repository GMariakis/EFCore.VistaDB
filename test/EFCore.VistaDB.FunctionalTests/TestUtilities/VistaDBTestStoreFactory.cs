// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.TestUtilities;

public class VistaDBTestStoreFactory : RelationalTestStoreFactory
{
    public static VistaDBTestStoreFactory Instance { get; } = new();

    protected VistaDBTestStoreFactory()
    {
    }

    public override TestStore Create(string storeName)
        => VistaDBTestStore.Create(storeName);

    public override TestStore GetOrCreate(string storeName)
        => VistaDBTestStore.GetOrCreate(storeName);

    public override IServiceCollection AddProviderServices(IServiceCollection serviceCollection)
        => serviceCollection.AddEntityFrameworkVistaDB();
}
