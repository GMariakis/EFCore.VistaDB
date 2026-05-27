// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDB.ValueGeneration.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public interface IVistaDBSequenceValueGeneratorFactory
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="clrType">The CLR type of the property.</param>
    /// <param name="connection">The VistaDB connection.</param>
    /// <param name="rawSqlCommandBuilder">A raw SQL command builder.</param>
    /// <param name="commandLogger">A command diagnostics logger.</param>
    /// <returns>The value generator (or <see langword="null" />).</returns>
    ValueGenerator? TryCreate(
        IProperty property,
        Type clrType,
        IVistaDBConnection connection,
        IRawSqlCommandBuilder rawSqlCommandBuilder,
        IRelationalCommandDiagnosticsLogger commandLogger);

    // VistaDB: no analog — the SqlServer signature carries a SqlServerSequenceValueGeneratorState
    // parameter. VistaDB has no sequences, so the state parameter is omitted and any caller will
    // receive a NotSupportedException from the implementation.
    // Original SqlServer signature preserved below for future revival.
    /*
    ValueGenerator? TryCreate(
        IProperty property,
        Type clrType,
        SqlServerSequenceValueGeneratorState generatorState,
        ISqlServerConnection connection,
        IRawSqlCommandBuilder rawSqlCommandBuilder,
        IRelationalCommandDiagnosticsLogger commandLogger);
    */
}
