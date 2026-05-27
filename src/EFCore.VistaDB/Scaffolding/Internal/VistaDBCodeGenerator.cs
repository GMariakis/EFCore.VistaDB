// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Scaffolding.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
/// <remarks>
///     VistaDB-specific provider code generator. Emits a <c>.UseVistaDB("Data Source=...")</c> call into
///     the scaffolded <c>OnConfiguring</c> override.
/// </remarks>
public class VistaDBCodeGenerator : ProviderCodeGenerator
{
    // VistaDB: pending — once VistaDBDbContextOptionsExtensions.UseVistaDB is added in a later task we should
    // switch to the strongly-typed MethodInfo overload, mirroring SqlServerCodeGenerator. For now we emit the
    // call by name; the scaffolded output is text and only resolves when the user's project compiles.
    // Original SqlServer logic preserved below for future revival.
    /*
    private static readonly MethodInfo UseSqlServerMethodInfo
        = typeof(SqlServerDbContextOptionsExtensions).GetRuntimeMethod(
            nameof(SqlServerDbContextOptionsExtensions.UseSqlServer),
            [typeof(DbContextOptionsBuilder), typeof(string), typeof(Action<SqlServerDbContextOptionsBuilder>)])!;
    */
    private const string UseVistaDBMethodName = "UseVistaDB";

    /// <summary>
    ///     Initializes a new instance of the <see cref="VistaDBCodeGenerator" /> class.
    /// </summary>
    /// <param name="dependencies">The dependencies.</param>
    public VistaDBCodeGenerator(ProviderCodeGeneratorDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override MethodCallCodeFragment GenerateUseProvider(
        string connectionString,
        MethodCallCodeFragment? providerOptions)
        => new(
            UseVistaDBMethodName,
            providerOptions == null
                ? [connectionString]
                : [connectionString, new NestedClosureCodeFragment("x", providerOptions)]);
}
