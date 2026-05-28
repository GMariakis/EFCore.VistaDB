// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

#nullable disable

public class CompositeKeyEndToEndVistaDBTest(CompositeKeyEndToEndVistaDBTest.CompositeKeyEndToEndVistaDBFixture fixture)
    : CompositeKeyEndToEndTestBase<
        CompositeKeyEndToEndVistaDBTest.CompositeKeyEndToEndVistaDBFixture>(fixture)
{
    public class CompositeKeyEndToEndVistaDBFixture : CompositeKeyEndToEndFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => VistaDBTestStoreFactory.Instance;
    }
}
