// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Design.Internal;

namespace Microsoft.EntityFrameworkCore;

#nullable disable

public class DesignTimeVistaDBTest(DesignTimeVistaDBTest.DesignTimeVistaDBFixture fixture)
    : DesignTimeTestBase<DesignTimeVistaDBTest.DesignTimeVistaDBFixture>(fixture)
{
    protected override Assembly ProviderAssembly
        => typeof(VistaDBDesignTimeServices).Assembly;

    public class DesignTimeVistaDBFixture : DesignTimeFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => VistaDBTestStoreFactory.Instance;
    }
}
