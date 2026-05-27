// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
/// <remarks>
///     The service lifetime is <see cref="ServiceLifetime.Scoped" />. This means that each
///     <see cref="DbContext" /> instance will use its own instance of this service.
///     The implementation may depend on other services registered with any lifetime.
///     The implementation does not need to be thread-safe.
/// </remarks>
public interface IVistaDBConnection : IRelationalConnection
{
    // VistaDB: no analog — VistaDB has no "master" database; databases are .vdb6 files on disk.
    // CreateMasterConnection / IsMultipleActiveResultSetsEnabled have no meaningful equivalents.
    // Original SqlServer surface preserved below for future revival.
    /*
        ISqlServerConnection CreateMasterConnection();

        bool IsMultipleActiveResultSetsEnabled { get; }
    */
}
