// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.EntityFrameworkCore.VistaDB.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBSqlGenerationHelper : RelationalSqlGenerationHelper
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBSqlGenerationHelper(RelationalSqlGenerationHelperDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override string StartTransactionStatement
        => "BEGIN TRANSACTION" + StatementTerminator;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override string EscapeIdentifier(string identifier)
        => SanitizeForVistaDB(identifier).Replace("]", "]]");

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override void EscapeIdentifier(StringBuilder builder, string identifier)
    {
        var sanitized = SanitizeForVistaDB(identifier);
        var initialLength = builder.Length;
        builder.Append(sanitized);
        builder.Replace("]", "]]", initialLength, sanitized.Length);
    }

    // VistaDB rejects certain characters in identifiers even when delimited with [brackets] —
    // notably '<', '>', '?', ',' which appear in EF Core test models that use generic CLR type
    // names (e.g. `BoolOnlyKey<bool?>`, `Tuple<int,string>`) as entity / table / PK constraint
    // names. SQL Server accepts these inside `[...]`; VistaDB does not. Sanitize them with a
    // deterministic, reversible-ish mapping so different generic instantiations don't collide.
    // Also handle VistaDB-reserved words that EF Core models use as column/table names
    // (e.g. "Save" — VistaDB reserves SAVE for SAVE TRANSACTION savepoint syntax).
    private static string SanitizeForVistaDB(string identifier)
    {
        if (s_reservedWords.Contains(identifier))
        {
            return identifier + "_";
        }

        if (identifier.AsSpan().IndexOfAny(s_illegalVistaDBIdentChars) < 0)
        {
            return identifier;
        }

        var sb = new StringBuilder(identifier.Length + 8);
        foreach (var c in identifier)
        {
            switch (c)
            {
                case '<':
                    sb.Append("_lt_");
                    break;
                case '>':
                    sb.Append("_gt_");
                    break;
                case '?':
                    sb.Append("_q_");
                    break;
                case ',':
                    sb.Append("_co_");
                    break;
                case ' ':
                    sb.Append('_');
                    break;
                case '~':
                    // EF Core's Uniquifier.Truncate appends '~' as the long-identifier truncation
                    // marker (e.g. CREATE INDEX [IX_Long…K~]). VistaDB rejects '~' in identifiers
                    // even when delimited — engine error 152 "Invalid name or alias" — verified by
                    // IdentifierLengthProbeTest. Map to '_' so the truncated name stays distinct
                    // from any non-truncated sibling but is parser-legal.
                    sb.Append('_');
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }

    private static readonly System.Buffers.SearchValues<char> s_illegalVistaDBIdentChars
        = System.Buffers.SearchValues.Create("<>?, ~");

    // VistaDB reserved words that the parser rejects as identifiers even inside [brackets].
    // SQL Server accepts these in delimited identifiers; VistaDB does not.
    // Extend this list as additional reserved-word collisions are discovered.
    // Compared case-insensitively because identifiers are folded to upper-case in matching.
    private static readonly HashSet<string> s_reservedWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SAVE",  // SAVE TRANSACTION (savepoint syntax)
    };

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override string DelimitIdentifier(string identifier)
        => $"[{EscapeIdentifier(identifier)}]"; // Interpolation okay; strings

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override void DelimitIdentifier(StringBuilder builder, string identifier)
    {
        builder.Append('[');
        EscapeIdentifier(builder, identifier);
        builder.Append(']');
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override string DelimitIdentifier(string name, string? schema)
    {
        if (!string.IsNullOrEmpty(schema)
            && !string.Equals(schema, "dbo", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(VistaDBStrings.SchemasNotSupported(name, schema));
        }

        return DelimitIdentifier(name);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override void DelimitIdentifier(StringBuilder builder, string name, string? schema)
    {
        if (!string.IsNullOrEmpty(schema)
            && !string.Equals(schema, "dbo", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(VistaDBStrings.SchemasNotSupported(name, schema));
        }

        DelimitIdentifier(builder, name);
    }

    /// <summary>
    ///     Generates an SQL statement which creates a savepoint with the given name.
    /// </summary>
    /// <param name="name">The name of the savepoint to be created.</param>
    /// <returns>An SQL string to create the savepoint.</returns>
    public override string GenerateCreateSavepointStatement(string name)
        => "SAVE TRANSACTION " + DelimitIdentifier(name) + StatementTerminator;

    /// <summary>
    ///     Generates an SQL statement which rolls back to a savepoint with the given name.
    /// </summary>
    /// <param name="name">The name of the savepoint to be rolled back to.</param>
    /// <returns>An SQL string to roll back the savepoint.</returns>
    public override string GenerateRollbackToSavepointStatement(string name)
        => "ROLLBACK TRANSACTION " + DelimitIdentifier(name) + StatementTerminator;

    // VistaDB: no analog — VistaDB, like SQL Server, does not support RELEASE SAVEPOINT.
    // Original SqlServer logic preserved below for future revival when VistaDB adds support.
    /*
        public override string GenerateReleaseSavepointStatement(string name)
            => throw new NotSupportedException(SqlServerStrings.NoSavepointRelease);

        public override string DelimitJsonPathElement(string pathElement)
            => !char.IsAsciiLetter(pathElement[0])
                ? $"\"{EscapeJsonPathElement(pathElement)}\""
                : base.DelimitJsonPathElement(pathElement);
    */
}
