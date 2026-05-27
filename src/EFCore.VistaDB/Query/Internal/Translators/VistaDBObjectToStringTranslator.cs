// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Microsoft.EntityFrameworkCore.VistaDB.Query.Internal;

/// <summary>
///     Translates <c>.ToString()</c> calls on primitive CLR types to <c>CONVERT(varchar(N), value)</c>.
///     VistaDB supports the SQL Server-compatible two-argument CONVERT, so this is a direct
///     port of <c>SqlServerObjectToStringTranslator</c>.
///
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBObjectToStringTranslator : IMethodCallTranslator
{
    // VistaDB does not allow parameterized types in CAST (e.g. CAST(x AS varchar(3)) is rejected
    // with parse error 632). Use bare nvarchar (no size specifier) for all conversions — VistaDB
    // will use a default max length. The values produced are semantically identical to SQL Server's
    // CAST(x AS varchar(N)) for the small numeric types used in practice (byte, int, etc.).
    private static readonly Dictionary<Type, string> TypeMapping
        = new()
        {
            { typeof(sbyte), "nvarchar" },
            { typeof(byte), "nvarchar" },
            { typeof(short), "nvarchar" },
            { typeof(ushort), "nvarchar" },
            { typeof(int), "nvarchar" },
            { typeof(uint), "nvarchar" },
            { typeof(long), "nvarchar" },
            { typeof(ulong), "nvarchar" },
            { typeof(float), "nvarchar" },
            { typeof(double), "nvarchar" },
            { typeof(decimal), "nvarchar" },
            { typeof(char), "nvarchar" },
            { typeof(DateTime), "nvarchar" },
            { typeof(DateOnly), "nvarchar" },
            { typeof(TimeOnly), "nvarchar" },
            { typeof(DateTimeOffset), "nvarchar" },
            { typeof(TimeSpan), "nvarchar" },
            { typeof(Guid), "nvarchar" },
            { typeof(byte[]), "nvarchar" }
        };

    private readonly ISqlExpressionFactory _sqlExpressionFactory;
    private readonly IRelationalTypeMappingSource _typeMappingSource;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBObjectToStringTranslator(
        ISqlExpressionFactory sqlExpressionFactory,
        IRelationalTypeMappingSource typeMappingSource)
    {
        _sqlExpressionFactory = sqlExpressionFactory;
        _typeMappingSource = typeMappingSource;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual SqlExpression? Translate(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (instance == null || method.Name != nameof(ToString) || arguments.Count != 0)
        {
            return null;
        }

        if (instance.TypeMapping?.ClrType == typeof(string))
        {
            return instance;
        }

        if (instance.Type == typeof(bool))
        {
            if (instance is not ColumnExpression { IsNullable: false })
            {
                return _sqlExpressionFactory.Case(
                    instance,
                    [
                        new CaseWhenClause(
                            _sqlExpressionFactory.Constant(false),
                            _sqlExpressionFactory.Constant(false.ToString())),
                        new CaseWhenClause(
                            _sqlExpressionFactory.Constant(true),
                            _sqlExpressionFactory.Constant(true.ToString()))
                    ],
                    _sqlExpressionFactory.Constant(string.Empty));
            }

            return _sqlExpressionFactory.Case(
                [
                    new CaseWhenClause(
                        instance,
                        _sqlExpressionFactory.Constant(true.ToString()))
                ],
                _sqlExpressionFactory.Constant(false.ToString()));
        }

        // Enums are handled by EnumMethodTranslator

        // Use CAST(value AS varchar(N)) — standard SQL supported by VistaDB.
        // VistaDB does not support CONVERT(varchar(N), value) — the parenthesised type name inside
        // CONVERT triggers a parse error (Error 632).
        // We omit the outer COALESCE(CAST(...), '') wrapper because VistaDB does not reject NULLs
        // in non-nullable contexts here, and the COALESCE(func(...), N'') can trigger a parser error
        // when the result appears in ORDER BY or sub-expressions in some VistaDB versions.
        return TypeMapping.TryGetValue(instance.Type, out var storeType)
            ? _sqlExpressionFactory.Convert(
                instance,
                typeof(string),
                _typeMappingSource.GetMapping(storeType))
            : null;
    }
}
