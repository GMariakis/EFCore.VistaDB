// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

#nullable disable

public class WithConstructorsVistaDBTest(WithConstructorsVistaDBTest.WithConstructorsVistaDBFixture fixture)
    : WithConstructorsTestBase<WithConstructorsVistaDBTest.WithConstructorsVistaDBFixture>(fixture)
{
    protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        => facade.UseTransaction(transaction.GetDbTransaction());

    [ConditionalFact]
    public override Task Query_and_update_using_constructors_with_property_parameters()
        => base.Query_and_update_using_constructors_with_property_parameters();

    [ConditionalFact]
    public override void Query_with_keyless_type() => base.Query_with_keyless_type();

    [ConditionalFact]
    public override void Query_with_context_injected() => base.Query_with_context_injected();

    [ConditionalFact]
    public override void Query_with_context_injected_into_property() => base.Query_with_context_injected_into_property();

    [ConditionalFact]
    public override void Query_with_context_injected_into_constructor_with_property()
        => base.Query_with_context_injected_into_constructor_with_property();

    [ConditionalFact]
    public override void Attaching_entity_sets_context() => base.Attaching_entity_sets_context();

    [ConditionalFact]
    public override void Query_with_EntityType_injected() => base.Query_with_EntityType_injected();

    [ConditionalFact]
    public override void Query_with_EntityType_injected_into_property() => base.Query_with_EntityType_injected_into_property();

    [ConditionalFact]
    public override void Query_with_StateManager_injected() => base.Query_with_StateManager_injected();

    [ConditionalFact]
    public override void Attaching_entity_sets_StateManager() => base.Attaching_entity_sets_StateManager();

    public class WithConstructorsVistaDBFixture : WithConstructorsFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => VistaDBTestStoreFactory.Instance;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            modelBuilder.Entity<BlogQuery>().HasNoKey().ToSqlQuery("SELECT * FROM Blog");
        }
    }
}
