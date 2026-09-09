// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.VistaDB.Scaffolding.Internal;

namespace Microsoft.EntityFrameworkCore;

public class ScaffoldingVistaDBTest
{
    // Was skipped for error 219, blamed on the engine caching a SingleProcessReadWrite reservation from
    // CreateDatabase that outlived its handle. That was not it. The reservation came from the provider
    // itself: EnsureCreated ran HasTables and the DDA accessor, both of which hardcoded SingleProcess,
    // against a connection the provider had augmented to MultiProcess. The scaffolding factory then asked
    // for MultiProcess and the engine refused, because the provider's own handles had already put the
    // file in the other family.
    //
    // Nothing about scaffolding needed fixing. Once every handle derives its mode from the connection
    // string (VistaDBOpenModes), this passes.
    [VistaDBInstalledFact]
    public void Reverse_engineer_simple_table_yields_expected_database_model()
    {
        using var file = new TempVistaDBFile();

        // Create the database via the EF Core path.
        using (var ctx = new ScaffoldContext(file.ConnectionString))
        {
            Assert.True(ctx.Database.EnsureCreated());
        }

        // Hand-create the Customer table via raw SQL through the underlying VistaDB ADO.NET client.
        // Use MultiProcessReadWrite (matching the EF Core default) so the connection coexists with
        // the parallel DDA handle that the database model factory opens later.
        using (var connection = new global::VistaDB.Provider.VistaDBConnection(
            file.ConnectionString + ";Open Mode=MultiProcessReadWrite"))
        {
            connection.Open();
            using var cmd = connection.CreateCommand();
            // VistaDB requires the explicit seed/increment form IDENTITY(1,1); bare IDENTITY is not accepted.
            cmd.CommandText =
                "CREATE TABLE Customer (Id INT IDENTITY(1,1) PRIMARY KEY, Name NVARCHAR(100) NOT NULL)";
            cmd.ExecuteNonQuery();
        }

        // Wire up the database model factory with a minimal type-mapping source and logger.
        var loggerFactory = new LoggerFactory();
        var diagnosticListener = new DiagnosticListener("EFCore.VistaDB.Test");
        var loggingDefinitions = new VistaDB.Diagnostics.Internal.VistaDBLoggingDefinitions();
        var logger = new DiagnosticsLogger<DbLoggerCategory.Scaffolding>(
            loggerFactory, new LoggingOptions(), diagnosticListener, loggingDefinitions, new NullDbContextLogger());

        var typeMappingSource = new VistaDB.Storage.Internal.VistaDBTypeMappingSource(
            TestServiceFactory.Instance.Create<TypeMappingSourceDependencies>(),
            TestServiceFactory.Instance.Create<RelationalTypeMappingSourceDependencies>(),
            TestServiceFactory.Instance.Create<VistaDB.Infrastructure.Internal.VistaDBSingletonOptions>());

        var factory = new VistaDBDatabaseModelFactory(logger, typeMappingSource);

        var dbModel = factory.Create(
            file.ConnectionString,
            new DatabaseModelFactoryOptions(tables: new[] { "Customer" }, schemas: Array.Empty<string>()));

        var table = Assert.Single(dbModel.Tables);
        Assert.Equal("Customer", table.Name);
        Assert.Equal(2, table.Columns.Count);
        Assert.Contains(table.Columns, c => c.Name == "Id");
        Assert.Contains(table.Columns, c => c.Name == "Name");
        Assert.NotNull(table.PrimaryKey);
        Assert.Contains(table.PrimaryKey.Columns, c => c.Name == "Id");

        // Cleanup: drop the file using EF's EnsureDeleted path.
        using (var ctx = new ScaffoldContext(file.ConnectionString))
        {
            ctx.Database.EnsureDeleted();
        }
    }

    private class ScaffoldContext : DbContext
    {
        private readonly string _connectionString;

        public ScaffoldContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseVistaDB(_connectionString);
    }
}
