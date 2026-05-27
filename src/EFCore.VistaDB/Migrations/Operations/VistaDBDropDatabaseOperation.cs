// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore.Migrations.Operations;

/// <summary>
///     A VistaDB-specific <see cref="MigrationOperation" /> to drop a database (deletes the <c>.vdb6</c> file).
/// </summary>
/// <remarks>
///     Mirrors <c>SqlServerDropDatabaseOperation</c>. VistaDB has no <c>DROP DATABASE</c> SQL — the generator
///     emits a Tier-2 DDA command that closes pooled connections and deletes the file.
/// </remarks>
public class VistaDBDropDatabaseOperation : MigrationOperation
{
    /// <summary>
    ///     The logical name of the database. For VistaDB the file path is taken from the current
    ///     <see cref="Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal.IVistaDBConnection" />.
    /// </summary>
    public virtual string Name { get; set; } = null!;
}
