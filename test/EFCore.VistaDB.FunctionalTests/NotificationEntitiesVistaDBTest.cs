// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

#nullable disable

public class NotificationEntitiesVistaDBTest(NotificationEntitiesVistaDBTest.NotificationEntitiesVistaDBFixture fixture)
    : NotificationEntitiesTestBase<NotificationEntitiesVistaDBTest.NotificationEntitiesVistaDBFixture>(fixture)
{
    public class NotificationEntitiesVistaDBFixture : NotificationEntitiesFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => VistaDBTestStoreFactory.Instance;
    }
}
