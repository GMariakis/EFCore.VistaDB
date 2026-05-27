// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ReSharper disable once CheckNamespace

namespace Microsoft.EntityFrameworkCore.Metadata;

/// <summary>
///     Defines strategies to use across the EF Core stack when generating key values
///     from VistaDB database columns.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-conventions">Model building conventions</see> for more information.
/// </remarks>
public enum VistaDBValueGenerationStrategy
{
    /// <summary>
    ///     No VistaDB-specific strategy
    /// </summary>
    None,

    /// <summary>
    ///     A pattern that uses a normal VistaDB <c>Identity</c> column.
    /// </summary>
    IdentityColumn,

    // VistaDB: no analog — VistaDB has no support for database sequences (CREATE SEQUENCE),
    // so neither the SequenceHiLo nor Sequence strategies are available.
    // Original SqlServer enum members preserved below for future revival when VistaDB adds support.
    /*
    SequenceHiLo,
    Sequence
    */
}
