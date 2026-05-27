// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Design.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Scaffolding.Internal;

[assembly: DesignTimeProviderServices("Microsoft.EntityFrameworkCore.VistaDB.Design.Internal.VistaDBDesignTimeServices")]

namespace Microsoft.EntityFrameworkCore.VistaDB.Design.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
/// <remarks>
///     Discovered reflectively by <c>dotnet ef</c> through the <c>[DesignTimeProviderServices]</c> assembly
///     attribute above. Wires the VistaDB-specific scaffolding services (<see cref="VistaDBDatabaseModelFactory" />,
///     <see cref="VistaDBCodeGenerator" />, <see cref="VistaDBAnnotationCodeGenerator" />) into the design-time
///     service collection.
/// </remarks>
public class VistaDBDesignTimeServices : IDesignTimeServices
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual void ConfigureDesignTimeServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddEntityFrameworkVistaDB();

#pragma warning disable EF1001 // Internal EF Core API usage.
        new EntityFrameworkRelationalDesignServicesBuilder(serviceCollection)
            .TryAdd<IAnnotationCodeGenerator, VistaDBAnnotationCodeGenerator>()
#pragma warning restore EF1001 // Internal EF Core API usage.
            .TryAdd<IDatabaseModelFactory, VistaDBDatabaseModelFactory>()
            .TryAdd<IProviderConfigurationCodeGenerator, VistaDBCodeGenerator>()
            .TryAddCoreServices();
    }
}
