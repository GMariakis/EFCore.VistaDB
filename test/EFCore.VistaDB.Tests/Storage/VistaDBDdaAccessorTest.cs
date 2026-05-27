// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

namespace Microsoft.EntityFrameworkCore.Storage;

public class VistaDBDdaAccessorTest
{
    [ConditionalFact]
    public void IsResolved_is_false_before_first_DDA_access_and_Dispose_is_safe()
    {
        // We pass null for IVistaDBConnection because the constructor stores it without touching it.
        // Accessing the Dda property would trigger VistaDBEngine.Connections.OpenDDA() which requires the
        // native engine — we deliberately don't exercise that here.
        var accessor = new VistaDBDdaAccessor(connection: null);

        Assert.False(accessor.IsResolved);

        accessor.Dispose();

        // Dispose is idempotent.
        accessor.Dispose();
    }

    [ConditionalFact(Skip = "Requires live VistaDB engine to lazily resolve IVistaDBDDA.")]
    public void Dda_resolves_lazily_and_IsResolved_flips_to_true()
    {
        // TODO(EFCore.VistaDB): once we have a host with the VistaDB engine, exercise:
        //   accessor.Dda -> IVistaDBDDA non-null; accessor.IsResolved -> true; Dispose -> IsResolved false.
    }
}
