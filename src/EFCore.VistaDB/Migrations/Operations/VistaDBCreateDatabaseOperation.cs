// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore.Migrations.Operations;

/// <summary>
///     A VistaDB-specific <see cref="MigrationOperation" /> to create a database (a single <c>.vdb6</c> file).
/// </summary>
/// <remarks>
///     <para>
///         Mirrors <c>SqlServerCreateDatabaseOperation</c>. VistaDB has no <c>CREATE DATABASE</c> SQL — the
///         <see cref="VistaDB.Migrations.VistaDBMigrationsSqlGenerator" /> emits a Tier-2 DDA command that calls
///         <c>IVistaDBDDA.CreateDatabase(filePath, ...)</c>.
///     </para>
///     <para>
///         See <see href="https://aka.ms/efcore-docs-migrations">Database migrations</see> for more information and examples.
///     </para>
/// </remarks>
[DebuggerDisplay("CREATE DATABASE {Name}")]
public class VistaDBCreateDatabaseOperation : DatabaseOperation
{
    /// <summary>
    ///     The logical name of the database. For VistaDB this is informational; the on-disk identity is
    ///     <see cref="FileName" />.
    /// </summary>
    public virtual string Name { get; set; } = null!;

    /// <summary>
    ///     The file path of the <c>.vdb6</c> database file to create. If <see langword="null" />, the
    ///     generator falls back to the current connection's <c>Data Source</c>.
    /// </summary>
    public virtual string? FileName { get; set; }
}
