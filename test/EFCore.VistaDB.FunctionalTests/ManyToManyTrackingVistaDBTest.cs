// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

#nullable disable

public class ManyToManyTrackingVistaDBTest(ManyToManyTrackingVistaDBTest.ManyToManyTrackingVistaDBFixture fixture)
    : ManyToManyTrackingVistaDBTestBase<ManyToManyTrackingVistaDBTest.ManyToManyTrackingVistaDBFixture>(fixture)
{
    public class ManyToManyTrackingVistaDBFixture : ManyToManyTrackingVistaDBFixtureBase
    {
        protected override string StoreName
            => "ManyToManyTrackingVistaDBTest";
    }
}
