// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

#nullable disable

public class ConcurrencyDetectorDisabledVistaDBTest : ConcurrencyDetectorDisabledRelationalTestBase<
    ConcurrencyDetectorDisabledVistaDBTest.ConcurrencyDetectorVistaDBFixture>
{
    public ConcurrencyDetectorDisabledVistaDBTest(ConcurrencyDetectorVistaDBFixture fixture)
        : base(fixture)
        => Fixture.TestSqlLoggerFactory.Clear();

    protected override async Task ConcurrencyDetectorTest(Func<ConcurrencyDetectorDbContext, Task<object>> test)
    {
        await base.ConcurrencyDetectorTest(test);

        Assert.NotEmpty(Fixture.TestSqlLoggerFactory.SqlStatements);
    }

    [ConditionalTheory]
    public override Task SaveChanges(bool async) => base.SaveChanges(async);

    [ConditionalTheory]
    public override Task Find(bool async) => base.Find(async);

    [ConditionalTheory]
    public override Task Count(bool async) => base.Count(async);

    [ConditionalTheory]
    public override Task First(bool async) => base.First(async);

    [ConditionalTheory]
    public override Task Last(bool async) => base.Last(async);

    [ConditionalTheory]
    public override Task Single(bool async) => base.Single(async);

    [ConditionalTheory]
    public override Task Any(bool async) => base.Any(async);

    [ConditionalTheory]
    public override Task ToList(bool async) => base.ToList(async);

    public class ConcurrencyDetectorVistaDBFixture : ConcurrencyDetectorFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => VistaDBTestStoreFactory.Instance;

        public TestSqlLoggerFactory TestSqlLoggerFactory
            => (TestSqlLoggerFactory)ListLoggerFactory;

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => builder.EnableThreadSafetyChecks(enableChecks: false);
    }
}
