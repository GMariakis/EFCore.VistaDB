// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

#nullable disable

public class TwoDatabasesVistaDBTest(VistaDBFixture fixture) : TwoDatabasesTestBase(fixture), IClassFixture<VistaDBFixture>
{
    protected new VistaDBFixture Fixture
        => (VistaDBFixture)base.Fixture;

    protected override DbContextOptionsBuilder CreateTestOptions(
        DbContextOptionsBuilder optionsBuilder,
        bool withConnectionString = false,
        bool withNullConnectionString = false)
        => withConnectionString
            ? withNullConnectionString
                ? optionsBuilder.UseVistaDB((string)null)
                : optionsBuilder.UseVistaDB(DummyConnectionString)
            : optionsBuilder.UseVistaDB();

    protected override TwoDatabasesWithDataContext CreateBackingContext(string databaseName)
        => new(Fixture.CreateOptions(VistaDBTestStore.Create(databaseName)));

    protected override string DummyConnectionString
        => "Data Source=DoesNotExist.vdb6";
}
