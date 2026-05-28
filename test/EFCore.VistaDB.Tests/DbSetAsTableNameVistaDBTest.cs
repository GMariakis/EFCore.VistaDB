// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

public class DbSetAsTableNameVistaDBTest : DbSetAsTableNameTest
{
    protected override string GetTableName<TEntity>(DbContext context)
        => context.Model.FindEntityType(typeof(TEntity)).GetTableName();

    protected override string GetTableName<TEntity>(DbContext context, string entityTypeName)
        => context.Model.FindEntityType(entityTypeName).GetTableName();

    protected override SetsContext CreateContext()
        => new VistaDBSetsContext();

    protected override SetsContext CreateNamedTablesContext()
        => new VistaDBNamedTablesContextContext();

    protected class VistaDBSetsContext : SetsContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseInternalServiceProvider(VistaDBFixture.DefaultServiceProvider)
                .UseVistaDB("Data Source=Dummy.vdb6");
    }

    protected class VistaDBNamedTablesContextContext : NamedTablesContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseInternalServiceProvider(VistaDBFixture.DefaultServiceProvider)
                .UseVistaDB("Data Source=Dummy.vdb6");
    }
}
