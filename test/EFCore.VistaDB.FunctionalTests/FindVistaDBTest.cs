// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

#nullable disable

// VistaDB: SqlServer's FindSqlServerTest has ~200 lines of SQL baseline overrides (TOP(1), [bracket]
// delimiters, N'literal'). VistaDB produces different SQL — we drop the overrides and inherit only
// the behavioral spec tests.
public abstract class FindVistaDBTest : FindTestBase<FindVistaDBTest.FindVistaDBFixture>
{
    protected FindVistaDBTest(FindVistaDBFixture fixture)
        : base(fixture)
        => fixture.TestSqlLoggerFactory.Clear();

    [ConditionalFact]
    public override void Find_int_key_tracked() => base.Find_int_key_tracked();

    [ConditionalFact]
    public override void Find_int_key_from_store() => base.Find_int_key_from_store();

    [ConditionalFact]
    public override void Returns_null_for_int_key_not_in_store() => base.Returns_null_for_int_key_not_in_store();

    [ConditionalFact]
    public override void Find_nullable_int_key_tracked() => base.Find_nullable_int_key_tracked();

    [ConditionalFact]
    public override void Find_nullable_int_key_from_store() => base.Find_nullable_int_key_from_store();

    [ConditionalFact]
    public override void Find_string_key_tracked() => base.Find_string_key_tracked();

    [ConditionalFact]
    public override void Find_string_key_from_store() => base.Find_string_key_from_store();

    [ConditionalFact]
    public override void Find_composite_key_tracked() => base.Find_composite_key_tracked();

    [ConditionalFact]
    public override void Find_composite_key_from_store() => base.Find_composite_key_from_store();

    [ConditionalFact]
    public override void Find_base_type_tracked() => base.Find_base_type_tracked();

    [ConditionalFact]
    public override void Find_base_type_from_store() => base.Find_base_type_from_store();

    [ConditionalFact]
    public override void Find_derived_type_tracked() => base.Find_derived_type_tracked();

    [ConditionalFact]
    public override void Find_derived_type_from_store() => base.Find_derived_type_from_store();

    [ConditionalFact]
    public override void Find_shadow_key_tracked() => base.Find_shadow_key_tracked();

    [ConditionalFact]
    public override void Find_shadow_key_from_store() => base.Find_shadow_key_from_store();

    public class FindVistaDBTestSet(FindVistaDBFixture fixture) : FindVistaDBTest(fixture)
    {
        protected override TestFinder Finder { get; } = new FindViaSetFinder();
    }

    public class FindVistaDBTestContext(FindVistaDBFixture fixture) : FindVistaDBTest(fixture)
    {
        protected override TestFinder Finder { get; } = new FindViaContextFinder();
    }

    public class FindVistaDBTestNonGeneric(FindVistaDBFixture fixture) : FindVistaDBTest(fixture)
    {
        protected override TestFinder Finder { get; } = new FindViaNonGenericContextFinder();
    }

    public class FindVistaDBFixture : FindFixtureBase
    {
        public TestSqlLoggerFactory TestSqlLoggerFactory
            => (TestSqlLoggerFactory)ListLoggerFactory;

        protected override ITestStoreFactory TestStoreFactory
            => VistaDBTestStoreFactory.Instance;
    }
}
