// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Migrations;

public class VistaDBMigrationBuilderTest
{
    [ConditionalFact]
    public void ActiveProvider_is_VistaDB_when_using_VistaDB()
    {
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.VistaDB");
        Assert.Equal("Microsoft.EntityFrameworkCore.VistaDB", migrationBuilder.ActiveProvider);
    }

    [ConditionalFact]
    public void ActiveProvider_is_not_VistaDB_when_using_different_provider()
    {
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.InMemory");
        Assert.NotEqual("Microsoft.EntityFrameworkCore.VistaDB", migrationBuilder.ActiveProvider);
    }

    // VistaDB: no analog — VistaDB does not yet provide a MigrationBuilder.IsVistaDB() helper.
    // Original SqlServer test preserved below for future revival when VistaDB adds support.
    /*
    [ConditionalFact]
    public void IsSqlServer_when_using_SqlServer()
    {
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        Assert.True(migrationBuilder.IsSqlServer());
    }
    */
}
