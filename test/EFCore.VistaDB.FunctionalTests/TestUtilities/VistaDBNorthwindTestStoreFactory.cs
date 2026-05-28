// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.TestUtilities;

/// <summary>
///     Placeholder Northwind store factory for VistaDB. The SqlServer variant loads a hand-authored
///     Northwind.sql script; porting that script to VistaDB DDL is a separate exercise. For now this
///     factory just delegates to the standard empty-store creation path; tests that actually require
///     Northwind data are expected to be skipped or to author their own seed.
/// </summary>
public class VistaDBNorthwindTestStoreFactory : VistaDBTestStoreFactory
{
    public const string Name = "Northwind";
    public static readonly string NorthwindConnectionString = VistaDBTestStore.CreateConnectionString(Name);
    public static new VistaDBNorthwindTestStoreFactory Instance { get; } = new();

    protected VistaDBNorthwindTestStoreFactory()
    {
    }

    public override TestStore GetOrCreate(string storeName)
        => throw new NotImplementedException(
            "VistaDB: Northwind seeding has not yet been ported from SqlServer.Northwind.sql to "
            + "VistaDB DDL. Tests requiring Northwind data should be marked "
            + "[ConditionalFact(Skip=\"VistaDB: requires Northwind port\")] until this lands. "
            + "See README-OmittedTests.md.");
}
