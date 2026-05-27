// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using VistaDB.DDA;

namespace Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
/// <remarks>
///     <para>
///         VistaDB-specific Tier-2 hook exposing the VistaDB managed Direct Data Access (DDA) surface
///         (<c>IVistaDBDDA</c>, <c>IVistaDBDatabase</c>, <c>IVistaDBTable</c>) keyed to the provider's
///         current <see cref="IVistaDBConnection" />. There is no SqlServer analog — DDA is the way the
///         VistaDB engine exposes file/schema operations that SQL cannot express.
///     </para>
///     <para>
///         The service lifetime is <see cref="ServiceLifetime.Scoped" />. This means that each
///         <see cref="DbContext" /> instance will use its own instance of this service.
///     </para>
/// </remarks>
public interface IVistaDBDdaAccessor : IDisposable
{
    /// <summary>
    ///     The lazily-resolved <see cref="IVistaDBDDA" /> instance.
    /// </summary>
    IVistaDBDDA Dda { get; }

    /// <summary>
    ///     <see langword="true" /> if the underlying <see cref="IVistaDBDDA" /> has been resolved (and not yet disposed).
    /// </summary>
    bool IsResolved { get; }

    /// <summary>
    ///     Resolves the path of the <c>.vdb6</c> database file referenced by the bound connection's
    ///     <c>Data Source</c>. Throws <see cref="InvalidOperationException" /> if the connection has no
    ///     data source.
    /// </summary>
    string GetDatabaseFilePath();

    /// <summary>
    ///     Opens the VistaDB database file referenced by the bound connection.
    /// </summary>
    /// <param name="readOnly">If <see langword="true" />, opens in <c>SharedReadOnly</c> mode; otherwise <c>NonExclusiveReadWrite</c>.</param>
    /// <returns>An <see cref="IVistaDBDatabase" /> the caller must dispose.</returns>
    IVistaDBDatabase OpenDatabase(bool readOnly = false);

    /// <summary>
    ///     Opens the database, runs <paramref name="action" /> against it, and disposes the database on exit.
    /// </summary>
    /// <param name="action">The action to invoke against the opened database.</param>
    /// <param name="readOnly">If <see langword="true" />, opens in <c>SharedReadOnly</c> mode; otherwise <c>NonExclusiveReadWrite</c>.</param>
    void WithDatabase(Action<IVistaDBDatabase> action, bool readOnly = false);

    /// <summary>
    ///     Opens the database and the named table, runs <paramref name="action" /> against the table,
    ///     and disposes both on exit.
    /// </summary>
    /// <param name="tableName">The name of the table to open.</param>
    /// <param name="action">The action to invoke against the opened table.</param>
    /// <param name="readOnly">If <see langword="true" />, opens the database in read-only mode.</param>
    /// <param name="exclusive">If <see langword="true" />, opens the table exclusively.</param>
    void WithTable(string tableName, Action<IVistaDBTable> action, bool readOnly = false, bool exclusive = false);
}
