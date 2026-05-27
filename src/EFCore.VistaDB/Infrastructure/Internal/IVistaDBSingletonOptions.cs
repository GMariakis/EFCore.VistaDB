// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Infrastructure.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public interface IVistaDBSingletonOptions : ISingletonOptions
{
    // VistaDB: no analog — VistaDB has a single engine (file-based .vdb6), so there is no
    // engine-type discriminator or compatibility-level concept.
    // Original SqlServer surface preserved below for future revival if VistaDB introduces variants.
    /*
        public SqlServerEngineType EngineType { get; }
        public int SqlServerCompatibilityLevel { get; }
        public int AzureSqlCompatibilityLevel { get; }
        public int AzureSynapseCompatibilityLevel { get; }
        public bool SupportsJsonFunctions { get; }
        public bool SupportsJsonObjectArray { get; }
        public bool SupportsJsonType { get; }
    */
}
