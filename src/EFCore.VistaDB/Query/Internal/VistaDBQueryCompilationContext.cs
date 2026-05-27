// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.EntityFrameworkCore.VistaDB.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBQueryCompilationContext : RelationalQueryCompilationContext
{
    // VistaDB: no analog — VistaDB has no MARS (Multiple Active Result Sets); split-query buffering
    // therefore cannot be toggled by a MARS flag, and we always rely on the base IsBuffering logic.
    // Original SqlServer state preserved below for future revival.
    /*
        private readonly bool _multipleActiveResultSetsEnabled;
    */

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBQueryCompilationContext(
        QueryCompilationContextDependencies dependencies,
        RelationalQueryCompilationContextDependencies relationalDependencies,
        bool async)
        : this(dependencies, relationalDependencies, async, precompiling: false)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    [Experimental(EFDiagnostics.PrecompiledQueryExperimental)]
    public VistaDBQueryCompilationContext(
        QueryCompilationContextDependencies dependencies,
        RelationalQueryCompilationContextDependencies relationalDependencies,
        bool async,
        bool precompiling)
        : base(dependencies, relationalDependencies, async, precompiling)
    {
    }

    /// <inheritdoc />
    public override bool SupportsPrecompiledQuery
        => true;
}
