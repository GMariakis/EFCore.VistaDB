// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

#nullable disable

public class OverzealousInitializationVistaDBTest(
    OverzealousInitializationVistaDBTest.OverzealousInitializationVistaDBFixture fixture)
    : OverzealousInitializationTestBase<OverzealousInitializationVistaDBTest.OverzealousInitializationVistaDBFixture>(fixture)
{
    public class OverzealousInitializationVistaDBFixture : OverzealousInitializationFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => VistaDBTestStoreFactory.Instance;
    }
}
