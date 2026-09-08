// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using VistaDB;

namespace Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
/// <remarks>
///     <para>
///         The one place that decides how a <c>.vdb6</c> is opened, because the engine requires every
///         handle on a file to agree and there is no way to ask it what mode is already in force.
///     </para>
///     <para>
///         A file is opened here three ways — the ADO.NET connection, the DDA handle used for Tier-2
///         work such as identity insert and the DDL VistaDB has no SQL for, and the creator's
///         read-only table probe. Measured against 6.6.2, mixing families is what the engine refuses:
///         a MultiProcess handle sits happily beside another MultiProcess handle, in the same process
///         or a different one, and a SingleProcess handle beside another SingleProcess handle in the
///         same process — but a MultiProcess handle beside a SingleProcess one fails either way round,
///         with error 218 naming whichever asked for SingleProcess.
///     </para>
///     <para>
///         These three used to pick their own mode and carry a comment claiming the others matched.
///         Two of the three comments were wrong, which is how the ADO.NET connection ended up on
///         MultiProcess while the DDA handle asked for SingleProcess — the one combination that cannot
///         work. Deriving every mode from the connection string here is what makes them agree by
///         construction rather than by three comments hoping.
///     </para>
/// </remarks>
public static class VistaDBOpenModes
{
    /// <summary>
    ///     What a connection string that names no <c>Open Mode</c> gets.
    ///
    ///     MultiProcess because it is strictly the more permissive of the two: SingleProcess means
    ///     "shared inside this process and nowhere else", so it locks out a second application, any
    ///     VistaDB tooling, and a design-time <c>dotnet ef</c> command run against a live database.
    ///     Nothing about the single-process case is faster or safer here — it is only narrower.
    /// </summary>
    public const VistaDBDatabaseOpenMode Default = VistaDBDatabaseOpenMode.MultiProcessReadWrite;

    /// <summary>The connection-string keyword. VistaDB spells it with the space.</summary>
    public const string Keyword = "Open Mode";

    /// <summary>
    ///     The mode a DDA handle must use to sit alongside the ADO.NET connection built from this
    ///     connection string — the caller's own choice when it made one, otherwise the default.
    /// </summary>
    public static VistaDBDatabaseOpenMode ForDda(string? connectionString, bool readOnly)
    {
        var connectionMode = FromConnectionString(connectionString);
        return readOnly ? ReadOnlyForm(connectionMode) : ReadWriteForm(connectionMode);
    }

    /// <summary>
    ///     The mode named in the connection string, or <see cref="Default" /> when it names none or
    ///     names something unparseable — the same value <c>VistaDBConnection</c> would have written in.
    /// </summary>
    public static VistaDBDatabaseOpenMode FromConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return Default;
        }

        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };

        return builder.TryGetValue(Keyword, out var value)
            && Enum.TryParse<VistaDBDatabaseOpenMode>(value as string, ignoreCase: true, out var mode)
                ? mode
                : Default;
    }

    /// <summary>The read-write member of the same locking family.</summary>
    private static VistaDBDatabaseOpenMode ReadWriteForm(VistaDBDatabaseOpenMode mode)
        => mode switch
        {
            VistaDBDatabaseOpenMode.SingleProcessReadOnly => VistaDBDatabaseOpenMode.SingleProcessReadWrite,
            VistaDBDatabaseOpenMode.MultiProcessReadOnly => VistaDBDatabaseOpenMode.MultiProcessReadWrite,
            VistaDBDatabaseOpenMode.ExclusiveReadOnly => VistaDBDatabaseOpenMode.ExclusiveReadWrite,
            // Nonexclusive* is deprecated in VistaDB 6 and deliberately unmapped: a caller who names one
            // gets exactly what they named rather than a guess at its sibling.
            _ => mode,
        };

    /// <summary>
    ///     The read-only member of the same locking family. Staying in the family is the point: the
    ///     engine cares which family a handle belongs to, not whether it intends to write.
    /// </summary>
    private static VistaDBDatabaseOpenMode ReadOnlyForm(VistaDBDatabaseOpenMode mode)
        => mode switch
        {
            VistaDBDatabaseOpenMode.SingleProcessReadWrite => VistaDBDatabaseOpenMode.SingleProcessReadOnly,
            VistaDBDatabaseOpenMode.MultiProcessReadWrite => VistaDBDatabaseOpenMode.MultiProcessReadOnly,
            VistaDBDatabaseOpenMode.ExclusiveReadWrite => VistaDBDatabaseOpenMode.ExclusiveReadOnly,
            _ => mode,
        };
}
