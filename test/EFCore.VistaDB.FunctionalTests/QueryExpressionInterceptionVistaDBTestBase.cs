// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

#nullable disable

public abstract class QueryExpressionInterceptionVistaDBTestBase(
    QueryExpressionInterceptionVistaDBTestBase.InterceptionVistaDBFixtureBase fixture)
    : QueryExpressionInterceptionTestBase(fixture)
{
    public abstract class InterceptionVistaDBFixtureBase : InterceptionFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => VistaDBTestStoreFactory.Instance;

        protected override IServiceCollection InjectInterceptors(
            IServiceCollection serviceCollection,
            IEnumerable<IInterceptor> injectedInterceptors)
            => base.InjectInterceptors(serviceCollection.AddEntityFrameworkVistaDB(), injectedInterceptors);
    }

    public class QueryExpressionInterceptionVistaDBTest(QueryExpressionInterceptionVistaDBTest.InterceptionVistaDBFixture fixture)
        : QueryExpressionInterceptionVistaDBTestBase(fixture),
            IClassFixture<QueryExpressionInterceptionVistaDBTest.InterceptionVistaDBFixture>
    {
        public class InterceptionVistaDBFixture : InterceptionVistaDBFixtureBase
        {
            protected override string StoreName
                => "QueryExpressionInterception";

            protected override bool ShouldSubscribeToDiagnosticListener
                => false;
        }
    }

    public class QueryExpressionInterceptionWithDiagnosticsVistaDBTest(
        QueryExpressionInterceptionWithDiagnosticsVistaDBTest.InterceptionVistaDBFixture fixture)
        : QueryExpressionInterceptionVistaDBTestBase(fixture),
            IClassFixture<QueryExpressionInterceptionWithDiagnosticsVistaDBTest.InterceptionVistaDBFixture>
    {
        public class InterceptionVistaDBFixture : InterceptionVistaDBFixtureBase
        {
            protected override string StoreName
                => "QueryExpressionInterceptionWithDiagnostics";

            protected override bool ShouldSubscribeToDiagnosticListener
                => true;
        }
    }
}
