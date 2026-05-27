// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Infrastructure.Internal;
using VistaDBClient = global::VistaDB.Provider.VistaDBConnection;

// ReSharper disable InconsistentNaming
namespace Microsoft.EntityFrameworkCore;

public class VistaDBDbContextOptionsExtensionsTest
{
    [ConditionalFact]
    public void Can_add_extension_with_max_batch_size()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseVistaDB("Data Source=Crunchie.vdb6", b => b.MaxBatchSize(123));

        var extension = optionsBuilder.Options.Extensions.OfType<VistaDBOptionsExtension>().Single();

        Assert.Equal(123, extension.MaxBatchSize);
    }

    [ConditionalFact]
    public void Can_add_extension_with_command_timeout()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseVistaDB("Data Source=Crunchie.vdb6", b => b.CommandTimeout(30));

        var extension = optionsBuilder.Options.Extensions.OfType<VistaDBOptionsExtension>().Single();

        Assert.Equal(30, extension.CommandTimeout);
    }

    [ConditionalFact]
    public void Can_add_extension_with_connection_string()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseVistaDB("Data Source=Crunchie.vdb6");

        var extension = optionsBuilder.Options.Extensions.OfType<VistaDBOptionsExtension>().Single();

        Assert.Equal("Data Source=Crunchie.vdb6", extension.ConnectionString);
        Assert.Null(extension.Connection);
    }

    [ConditionalTheory, InlineData(false), InlineData(true)]
    public void Can_add_extension_with_connection_string_using_generic_options(bool nullConnectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DbContext>();
        optionsBuilder.UseVistaDB(nullConnectionString ? null : "Data Source=Whisper.vdb6");

        var extension = optionsBuilder.Options.Extensions.OfType<VistaDBOptionsExtension>().Single();

        Assert.Equal(nullConnectionString ? null : "Data Source=Whisper.vdb6", extension.ConnectionString);
        Assert.Null(extension.Connection);
    }

    [ConditionalFact]
    public void Can_add_extension_with_connection()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        var connection = new VistaDBClient();

        optionsBuilder.UseVistaDB(connection);

        var extension = optionsBuilder.Options.Extensions.OfType<VistaDBOptionsExtension>().Single();

        Assert.Same(connection, extension.Connection);
        Assert.False(extension.IsConnectionOwned);
        Assert.Null(extension.ConnectionString);
    }

    [ConditionalFact]
    public void Can_add_extension_with_owned_connection()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        var connection = new VistaDBClient();

        optionsBuilder.UseVistaDB(connection, contextOwnsConnection: true);

        var extension = optionsBuilder.Options.Extensions.OfType<VistaDBOptionsExtension>().Single();

        Assert.Same(connection, extension.Connection);
        Assert.True(extension.IsConnectionOwned);
        Assert.Null(extension.ConnectionString);
    }

    [ConditionalFact]
    public void Connection_overrides_connection_string()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        var connection = new VistaDBClient();

        optionsBuilder.UseVistaDB("Data Source=Whisper.vdb6");
        optionsBuilder.UseVistaDB(connection);

        var extension = optionsBuilder.Options.Extensions.OfType<VistaDBOptionsExtension>().Single();

        Assert.Same(connection, extension.Connection);
        Assert.False(extension.IsConnectionOwned);
        Assert.Null(extension.ConnectionString);
    }

    [ConditionalFact]
    public void Connection_string_overrides_connection()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        var connection = new VistaDBClient();

        optionsBuilder.UseVistaDB(connection);
        optionsBuilder.UseVistaDB("Data Source=Whisper.vdb6");

        var extension = optionsBuilder.Options.Extensions.OfType<VistaDBOptionsExtension>().Single();

        Assert.False(extension.IsConnectionOwned);
        Assert.Null(extension.Connection);
        Assert.Equal("Data Source=Whisper.vdb6", extension.ConnectionString);
    }

    [ConditionalFact]
    public void Can_add_extension_with_connection_using_generic_options()
    {
        var optionsBuilder = new DbContextOptionsBuilder<DbContext>();
        var connection = new VistaDBClient();

        optionsBuilder.UseVistaDB(connection);

        var extension = optionsBuilder.Options.Extensions.OfType<VistaDBOptionsExtension>().Single();

        Assert.Same(connection, extension.Connection);
        Assert.False(extension.IsConnectionOwned);
        Assert.Null(extension.ConnectionString);
    }

    [ConditionalFact]
    public void Can_add_extension_with_owned_connection_using_generic_options()
    {
        var optionsBuilder = new DbContextOptionsBuilder<DbContext>();
        var connection = new VistaDBClient();

        optionsBuilder.UseVistaDB(connection, contextOwnsConnection: true);

        var extension = optionsBuilder.Options.Extensions.OfType<VistaDBOptionsExtension>().Single();

        Assert.Same(connection, extension.Connection);
        Assert.True(extension.IsConnectionOwned);
        Assert.Null(extension.ConnectionString);
    }

    [ConditionalTheory, InlineData(false), InlineData(true)]
    public void Service_collection_extension_method_can_configure_vistadb_options(bool nullConnectionString)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddVistaDB<ApplicationDbContext>(
            nullConnectionString ? null : "Data Source=Crunchie.vdb6",
            vistaDbOption =>
            {
                vistaDbOption.MaxBatchSize(123);
                vistaDbOption.CommandTimeout(30);
            },
            dbContextOption =>
            {
                dbContextOption.EnableDetailedErrors();
            });

        var services = serviceCollection.BuildServiceProvider(validateScopes: true);

        using (var serviceScope = services
                   .GetRequiredService<IServiceScopeFactory>()
                   .CreateScope())
        {
            var coreOptions = serviceScope.ServiceProvider
                .GetRequiredService<DbContextOptions<ApplicationDbContext>>().GetExtension<CoreOptionsExtension>();

            Assert.True(coreOptions.DetailedErrorsEnabled);

            var vistaDbOptions = serviceScope.ServiceProvider
                .GetRequiredService<DbContextOptions<ApplicationDbContext>>().GetExtension<VistaDBOptionsExtension>();

            Assert.Equal(123, vistaDbOptions.MaxBatchSize);
            Assert.Equal(30, vistaDbOptions.CommandTimeout);
            Assert.Equal(nullConnectionString ? null : "Data Source=Crunchie.vdb6", vistaDbOptions.ConnectionString);
        }
    }

    // VistaDB: no analog — VistaDB has no Azure SQL / Azure Synapse engine variants.
    // Original SqlServer Azure-engine test cases preserved below for future revival when VistaDB adds support.
    /*
    [ConditionalFact]
    public void Default_engine_type_is_SqlServer()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseSqlServer("Database=Crunchie");

        var extension = optionsBuilder.Options.Extensions.OfType<SqlServerOptionsExtension>().Single();
        Assert.Equal(SqlServerEngineType.SqlServer, extension.EngineType);
    }
    */

    private class ApplicationDbContext(DbContextOptions options) : DbContext(options);
}
