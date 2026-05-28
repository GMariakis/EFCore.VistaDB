// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.EntityFrameworkCore;

public abstract class ConnectionInterceptionVistaDBTestBase(
    ConnectionInterceptionVistaDBTestBase.InterceptionVistaDBFixtureBase fixture)
    : ConnectionInterceptionTestBase(fixture)
{
    public abstract class InterceptionVistaDBFixtureBase : InterceptionFixtureBase
    {
        protected override string StoreName
            => "ConnectionInterception";

        protected override ITestStoreFactory TestStoreFactory
            => VistaDBTestStoreFactory.Instance;

        protected override IServiceCollection InjectInterceptors(
            IServiceCollection serviceCollection,
            IEnumerable<IInterceptor> injectedInterceptors)
            => base.InjectInterceptors(serviceCollection.AddEntityFrameworkVistaDB(), injectedInterceptors);
    }

    protected override DbContextOptionsBuilder ConfigureProvider(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseVistaDB();

    protected override BadUniverseContext CreateBadUniverse(DbContextOptionsBuilder optionsBuilder)
        => new(optionsBuilder.UseVistaDB(new FakeDbConnection()).Options);

    [ConditionalTheory]
    public override Task Intercept_connection_passively(bool async) => base.Intercept_connection_passively(async);

    [ConditionalTheory]
    public override Task Intercept_connection_to_override_opening(bool async)
        => base.Intercept_connection_to_override_opening(async);

    [ConditionalTheory]
    public override Task Intercept_connection_with_multiple_interceptors(bool async)
        => base.Intercept_connection_with_multiple_interceptors(async);

    [ConditionalTheory]
    public override Task Intercept_connection_that_throws_on_open(bool async)
        => base.Intercept_connection_that_throws_on_open(async);

    [ConditionalTheory]
    public override Task Intercept_connection_creation_passively(bool async)
        => base.Intercept_connection_creation_passively(async);

    [ConditionalTheory]
    public override Task Intercept_connection_to_override_creation(bool async)
        => base.Intercept_connection_to_override_creation(async);

    [ConditionalTheory]
    public override Task Intercept_connection_to_override_connection_after_creation(bool async)
        => base.Intercept_connection_to_override_connection_after_creation(async);

    [ConditionalTheory]
    public override Task Intercept_connection_to_suppress_dispose(bool async)
        => base.Intercept_connection_to_suppress_dispose(async);

    [ConditionalTheory]
    public override Task Intercept_connection_creation_with_multiple_interceptors(bool async)
        => base.Intercept_connection_creation_with_multiple_interceptors(async);

    public class FakeDbConnection : DbConnection
    {
        [AllowNull]
        public override string ConnectionString { get; set; }

        public override string Database
            => "Database";

        public override string DataSource
            => "DataSource";

        public override string ServerVersion
            => throw new NotImplementedException();

        public override ConnectionState State
            => ConnectionState.Closed;

        public override void ChangeDatabase(string databaseName)
            => throw new NotImplementedException();

        public override void Close()
            => throw new NotImplementedException();

        public override void Open()
            => throw new NotImplementedException();

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            => throw new NotImplementedException();

        protected override DbCommand CreateDbCommand()
            => throw new NotImplementedException();
    }

    public class ConnectionInterceptionVistaDBTest(ConnectionInterceptionVistaDBTest.InterceptionVistaDBFixture fixture)
        : ConnectionInterceptionVistaDBTestBase(fixture), IClassFixture<ConnectionInterceptionVistaDBTest.InterceptionVistaDBFixture>
    {
        public class InterceptionVistaDBFixture : InterceptionVistaDBFixtureBase
        {
            protected override bool ShouldSubscribeToDiagnosticListener
                => false;
        }
    }

    public class ConnectionInterceptionWithDiagnosticsVistaDBTest(
        ConnectionInterceptionWithDiagnosticsVistaDBTest.InterceptionVistaDBFixture fixture)
        : ConnectionInterceptionVistaDBTestBase(fixture),
            IClassFixture<ConnectionInterceptionWithDiagnosticsVistaDBTest.InterceptionVistaDBFixture>
    {
        public class InterceptionVistaDBFixture : InterceptionVistaDBFixtureBase
        {
            protected override bool ShouldSubscribeToDiagnosticListener
                => true;
        }
    }
}
