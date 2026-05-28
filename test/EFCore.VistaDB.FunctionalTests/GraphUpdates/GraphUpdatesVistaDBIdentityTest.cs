// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

#nullable disable

public class GraphUpdatesVistaDBIdentityTest(GraphUpdatesVistaDBIdentityTest.VistaDBFixture fixture)
    : GraphUpdatesVistaDBTestBase<GraphUpdatesVistaDBIdentityTest.VistaDBFixture>(fixture)
{
    protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        => facade.UseTransaction(transaction.GetDbTransaction());

    public class VistaDBFixture : GraphUpdatesVistaDBFixtureBase
    {
        protected override string StoreName
            => "GraphIdentityUpdatesTest";

        // VistaDB: no analog — model-level UseIdentityColumns() isn't surfaced (identity is the default
        // value-generation strategy for VistaDB key properties). The SqlServer fixture set the model
        // default explicitly; VistaDB derives the same behavior implicitly.
        /*
        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            modelBuilder.UseIdentityColumns();
            base.OnModelCreating(modelBuilder, context);
        }
        */
    }
}
