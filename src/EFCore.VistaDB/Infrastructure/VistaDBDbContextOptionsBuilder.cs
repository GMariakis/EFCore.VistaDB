// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Infrastructure.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDB.Infrastructure;

/// <summary>
///     Allows VistaDB specific configuration to be performed on <see cref="DbContextOptions" />.
/// </summary>
/// <remarks>
///     <para>
///         Instances of this class are returned from a call to <c>UseVistaDB</c>
///         and it is not designed to be directly constructed in your application code.
///     </para>
///     <para>
///         See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see> for more information and examples.
///     </para>
/// </remarks>
public class VistaDBDbContextOptionsBuilder
    : RelationalDbContextOptionsBuilder<VistaDBDbContextOptionsBuilder, VistaDBOptionsExtension>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="VistaDBDbContextOptionsBuilder" /> class.
    /// </summary>
    /// <param name="optionsBuilder">The options builder.</param>
    public VistaDBDbContextOptionsBuilder(DbContextOptionsBuilder optionsBuilder)
        : base(optionsBuilder)
    {
    }

    // VistaDB: no analog — VistaDB has no Azure SQL / Azure Synapse variant builders.
    // Original SqlServer SqlEngineDbContextOptionsBuilder / AzureSqlDbContextOptionsBuilder /
    // AzureSynapseDbContextOptionsBuilder surface preserved on the SqlServer provider for reference.
}
