// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.TestUtilities.FakeProvider;
using Microsoft.EntityFrameworkCore.VistaDB.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

namespace Microsoft.EntityFrameworkCore.Storage.Internal;

public class VistaDBConnectionTest
{
    [ConditionalFact]
    public void Creates_VistaDB_connection()
    {
        using var connection = new VistaDBConnection(CreateDependencies());
        Assert.IsType<global::VistaDB.Provider.VistaDBConnection>(connection.DbConnection);
    }

    // VistaDB: no analog — VistaDB has no "master" database; each .vdb6 file is self-contained.
    // The Application Name connection-string injection that SqlServerConnection performs also does
    // not apply.
    // Original SqlServer test preserved below for future revival when VistaDB adds support.
    /*
    [ConditionalFact]
    public void Can_create_master_connection()
    {
        using var connection = new SqlServerConnection(CreateDependencies());
        using var master = connection.CreateMasterConnection();
        Assert.Equal(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=master", StripApplicationName(master.ConnectionString));
        Assert.Equal(60, master.CommandTimeout);
    }
    */

    [ConditionalFact]
    public void Connection_string_is_passed_through_verbatim()
    {
        var options = new DbContextOptionsBuilder()
            .UseVistaDB("Data Source=VistaDBConnectionTest.vdb6")
            .Options;

        using var connection = new VistaDBConnection(CreateDependencies(options));
        Assert.Equal("Data Source=VistaDBConnectionTest.vdb6", connection.ConnectionString);

        connection.ConnectionString = "Data Source=SomeOther.vdb6";
        Assert.Equal("Data Source=SomeOther.vdb6", connection.ConnectionString);
    }

    public static RelationalConnectionDependencies CreateDependencies(DbContextOptions options = null)
    {
        options ??= new DbContextOptionsBuilder()
            .UseVistaDB("Data Source=VistaDBConnectionTest.vdb6")
            .Options;

        return new RelationalConnectionDependencies(
            options,
            new DiagnosticsLogger<DbLoggerCategory.Database.Transaction>(
                new LoggerFactory(),
                new LoggingOptions(),
                new DiagnosticListener("FakeDiagnosticListener"),
                new VistaDBLoggingDefinitions(),
                new NullDbContextLogger()),
            new RelationalConnectionDiagnosticsLogger(
                new LoggerFactory(),
                new LoggingOptions(),
                new DiagnosticListener("FakeDiagnosticListener"),
                new VistaDBLoggingDefinitions(),
                new NullDbContextLogger(),
                CreateOptions()),
            new NamedConnectionStringResolver(options),
            new RelationalTransactionFactory(
                new RelationalTransactionFactoryDependencies(
                    new RelationalSqlGenerationHelper(
                        new RelationalSqlGenerationHelperDependencies()))),
            new CurrentDbContext(new FakeDbContext()),
            new RelationalCommandBuilderFactory(
                new RelationalCommandBuilderDependencies(
                    new TestRelationalTypeMappingSource(
                        TestServiceFactory.Instance.Create<TypeMappingSourceDependencies>(),
                        TestServiceFactory.Instance.Create<RelationalTypeMappingSourceDependencies>()),
                    new VistaDBExceptionDetector(),
                    new LoggingOptions())),
            new VistaDBExceptionDetector());
    }

    private const string ConnectionString = "Fake Connection String";

    private static IDbContextOptions CreateOptions(
        RelationalOptionsExtension optionsExtension = null)
    {
        var optionsBuilder = new DbContextOptionsBuilder();

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder)
            .AddOrUpdateExtension(
                optionsExtension
                ?? new FakeRelationalOptionsExtension().WithConnectionString(ConnectionString));

        return optionsBuilder.Options;
    }

    private class FakeDbContext : DbContext;
}
