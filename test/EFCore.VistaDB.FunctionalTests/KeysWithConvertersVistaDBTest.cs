// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

#nullable disable

public class KeysWithConvertersVistaDBTest(KeysWithConvertersVistaDBTest.KeysWithConvertersVistaDBFixture fixture)
    : KeysWithConvertersTestBase<
        KeysWithConvertersVistaDBTest.KeysWithConvertersVistaDBFixture>(fixture)
{
    [ConditionalFact]
    public override Task Can_insert_and_read_back_with_struct_key_and_optional_dependents()
        => base.Can_insert_and_read_back_with_struct_key_and_optional_dependents();

    [ConditionalFact]
    public override Task Can_insert_and_read_back_with_comparable_struct_key_and_optional_dependents()
        => base.Can_insert_and_read_back_with_comparable_struct_key_and_optional_dependents();

    [ConditionalFact]
    public override Task Can_insert_and_read_back_with_generic_comparable_struct_key_and_optional_dependents()
        => base.Can_insert_and_read_back_with_generic_comparable_struct_key_and_optional_dependents();

    [ConditionalFact]
    public override Task Can_insert_and_read_back_with_struct_key_and_required_dependents()
        => base.Can_insert_and_read_back_with_struct_key_and_required_dependents();

    [ConditionalFact]
    public override Task Can_insert_and_read_back_with_class_key_and_optional_dependents()
        => base.Can_insert_and_read_back_with_class_key_and_optional_dependents();

    [ConditionalFact]
    public override Task Can_insert_and_read_back_with_bare_class_key_and_optional_dependents()
        => base.Can_insert_and_read_back_with_bare_class_key_and_optional_dependents();

    [ConditionalFact]
    public override Task Can_insert_and_read_back_with_comparable_class_key_and_optional_dependents()
        => base.Can_insert_and_read_back_with_comparable_class_key_and_optional_dependents();

    [ConditionalFact]
    public override Task Can_insert_and_read_back_with_struct_binary_key_and_optional_dependents()
        => base.Can_insert_and_read_back_with_struct_binary_key_and_optional_dependents();

    public class KeysWithConvertersVistaDBFixture : KeysWithConvertersFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => VistaDBTestStoreFactory.Instance;

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => builder.UseVistaDB(b => b.MinBatchSize(1));
    }
}
