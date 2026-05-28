// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

#nullable disable

public class MusicStoreVistaDBTest(MusicStoreVistaDBTest.MusicStoreVistaDBFixture fixture)
    : MusicStoreTestBase<MusicStoreVistaDBTest.MusicStoreVistaDBFixture>(fixture)
{
    // VistaDB rejects scalar correlated subqueries in ORDER BY (parse error 567/509).
    // The query "OrderByDescending(a => a.OrderDetails.Count)" translates to
    // ORDER BY (SELECT COUNT(*) FROM OrderDetails WHERE AlbumId = a.AlbumId) DESC,
    // which VistaDB cannot process. Document as a known limitation.
    [ConditionalFact(Skip = "VistaDB: scalar correlated subquery in ORDER BY not supported (Error 567).")]
    public override Task Index_GetsSixTopAlbums()
        => base.Index_GetsSixTopAlbums();

    public class MusicStoreVistaDBFixture : MusicStoreFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => VistaDBTestStoreFactory.Instance;
    }
}
