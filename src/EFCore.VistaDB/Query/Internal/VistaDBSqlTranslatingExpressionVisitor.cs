// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using ExpressionExtensions = Microsoft.EntityFrameworkCore.Query.ExpressionExtensions;

namespace Microsoft.EntityFrameworkCore.VistaDB.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBSqlTranslatingExpressionVisitor : RelationalSqlTranslatingExpressionVisitor
{
    private readonly VistaDBQueryCompilationContext _queryCompilationContext;
    private readonly ISqlExpressionFactory _sqlExpressionFactory;

    private static readonly HashSet<string> DateTimeDataTypes
        =
        [
            "time",
            "date",
            "datetime",
            "datetime2",
            "datetimeoffset"
        ];

    private static readonly HashSet<Type> DateTimeClrTypes
        =
        [
            typeof(TimeOnly),
            typeof(DateOnly),
            typeof(TimeSpan),
            typeof(DateTime),
            typeof(DateTimeOffset)
        ];

    private static readonly HashSet<ExpressionType> ArithmeticOperatorTypes
        =
        [
            ExpressionType.Add,
            ExpressionType.Subtract,
            ExpressionType.Multiply,
            ExpressionType.Divide,
            ExpressionType.Modulo
        ];

    private static readonly MethodInfo StringStartsWithMethodInfoString
        = typeof(string).GetRuntimeMethod(nameof(string.StartsWith), [typeof(string)])!;

    private static readonly MethodInfo StringStartsWithMethodInfoChar
        = typeof(string).GetRuntimeMethod(nameof(string.StartsWith), [typeof(char)])!;

    private static readonly MethodInfo StringEndsWithMethodInfoString
        = typeof(string).GetRuntimeMethod(nameof(string.EndsWith), [typeof(string)])!;

    private static readonly MethodInfo StringEndsWithMethodInfoChar
        = typeof(string).GetRuntimeMethod(nameof(string.EndsWith), [typeof(char)])!;

    private static readonly MethodInfo StringContainsMethodInfoString
        = typeof(string).GetRuntimeMethod(nameof(string.Contains), [typeof(string)])!;

    private static readonly MethodInfo StringContainsMethodInfoChar
        = typeof(string).GetRuntimeMethod(nameof(string.Contains), [typeof(char)])!;

    private static readonly MethodInfo EscapeLikePatternParameterMethod =
        typeof(VistaDBSqlTranslatingExpressionVisitor).GetTypeInfo().GetDeclaredMethod(nameof(ConstructLikePatternParameter))!;

    private const char LikeEscapeChar = '\\';
    private const string LikeEscapeString = "\\";

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBSqlTranslatingExpressionVisitor(
        RelationalSqlTranslatingExpressionVisitorDependencies dependencies,
        VistaDBQueryCompilationContext queryCompilationContext,
        QueryableMethodTranslatingExpressionVisitor queryableMethodTranslatingExpressionVisitor)
        : base(dependencies, queryCompilationContext, queryableMethodTranslatingExpressionVisitor)
    {
        _queryCompilationContext = queryCompilationContext;
        _sqlExpressionFactory = dependencies.SqlExpressionFactory;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitBinary(BinaryExpression binaryExpression)
    {
        if (binaryExpression.NodeType == ExpressionType.ArrayIndex
            && binaryExpression.Left.Type == typeof(byte[]))
        {
            return TranslateByteArrayElementAccess(
                binaryExpression.Left,
                binaryExpression.Right,
                binaryExpression.Type);
        }

        var visitedExpression = base.VisitBinary(binaryExpression);

        if (visitedExpression is SqlBinaryExpression sqlBinaryExpression
            && ArithmeticOperatorTypes.Contains(sqlBinaryExpression.OperatorType))
        {
            var inferredProviderType = GetProviderType(sqlBinaryExpression.Left) ?? GetProviderType(sqlBinaryExpression.Right);
            if (inferredProviderType != null)
            {
                if (DateTimeDataTypes.Contains(inferredProviderType))
                {
                    return QueryCompilationContext.NotTranslatedExpression;
                }
            }
            else
            {
                var leftType = sqlBinaryExpression.Left.Type;
                var rightType = sqlBinaryExpression.Right.Type;
                if (DateTimeClrTypes.Contains(leftType)
                    || DateTimeClrTypes.Contains(rightType))
                {
                    return QueryCompilationContext.NotTranslatedExpression;
                }
            }
        }

        return visitedExpression;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitUnary(UnaryExpression unaryExpression)
    {
        if (unaryExpression.NodeType == ExpressionType.ArrayLength
            && unaryExpression.Operand.Type == typeof(byte[]))
        {
            if (!(base.Visit(unaryExpression.Operand) is SqlExpression sqlExpression))
            {
                return QueryCompilationContext.NotTranslatedExpression;
            }

            var isBinaryMaxDataType = GetProviderType(sqlExpression) == "varbinary(max)" || sqlExpression is SqlParameterExpression;
            var dataLengthSqlFunction = Dependencies.SqlExpressionFactory.Function(
                "DATALENGTH",
                [sqlExpression],
                nullable: true,
                argumentsPropagateNullability: Statics.TrueArrays[1],
                isBinaryMaxDataType ? typeof(long) : typeof(int));

            return isBinaryMaxDataType
                ? Dependencies.SqlExpressionFactory.Convert(dataLengthSqlFunction, typeof(int))
                : dataLengthSqlFunction;
        }

        return base.VisitUnary(unaryExpression);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
    {
        var method = methodCallExpression.Method;

        if (method.IsGenericMethod
            && method.GetGenericMethodDefinition() == EnumerableMethods.ElementAt
            && methodCallExpression.Arguments[0].Type == typeof(byte[]))
        {
            return TranslateByteArrayElementAccess(
                methodCallExpression.Arguments[0],
                methodCallExpression.Arguments[1],
                methodCallExpression.Type);
        }

        // Translate byte[].First() as SUBSTRING(arr, 1, 1) — the same semantics as ElementAt(arr, 0).
        // Mirrors SqlServer's ElementAt handling; SUBSTRING is supported by VistaDB.
        if (method.IsGenericMethod
            && method.GetGenericMethodDefinition() == EnumerableMethods.FirstWithoutPredicate
            && methodCallExpression.Arguments[0].Type == typeof(byte[]))
        {
            return TranslateByteArrayElementAccess(
                methodCallExpression.Arguments[0],
                Expression.Constant(0),
                methodCallExpression.Type);
        }

        if ((method == StringStartsWithMethodInfoString || method == StringStartsWithMethodInfoChar)
            && TryTranslateStartsEndsWithContains(
                methodCallExpression.Object!, methodCallExpression.Arguments[0], StartsEndsWithContains.StartsWith, out var translation1))
        {
            return translation1;
        }

        if ((method == StringEndsWithMethodInfoString || method == StringEndsWithMethodInfoChar)
            && TryTranslateStartsEndsWithContains(
                methodCallExpression.Object!, methodCallExpression.Arguments[0], StartsEndsWithContains.EndsWith, out var translation2))
        {
            return translation2;
        }

        if ((method == StringContainsMethodInfoString || method == StringContainsMethodInfoChar)
            && TryTranslateStartsEndsWithContains(
                methodCallExpression.Object!, methodCallExpression.Arguments[0], StartsEndsWithContains.Contains, out var translation3))
        {
            return translation3;
        }

        // VistaDB: no analog — SqlServer translates non-aggregate string.Join to CONCAT_WS (SqlServer 2017+ /
        // Azure Synapse). VistaDB does not support CONCAT_WS, so we let the base "could not translate" surface.
        // Original SqlServer logic preserved below for future revival when VistaDB adds CONCAT_WS support.
        /*
            private static readonly MethodInfo StringJoinMethodInfo
                = typeof(string).GetRuntimeMethod(nameof(string.Join), [typeof(string), typeof(string[])])!;

            if (method == StringJoinMethodInfo
                && methodCallExpression.Arguments[1] is NewArrayExpression newArrayExpression
                && ...compat-level check...)
            {
                // ... builds a CONCAT_WS(delimiter, arg1, arg2, ...) call, coalescing nulls to empty strings.
            }
        */

        return base.VisitMethodCall(methodCallExpression);

        bool TryTranslateStartsEndsWithContains(
            Expression instance,
            Expression pattern,
            StartsEndsWithContains methodType,
            [NotNullWhen(true)] out SqlExpression? translation)
        {
            if (Visit(instance) is not SqlExpression translatedInstance
                || Visit(pattern) is not SqlExpression translatedPattern)
            {
                translation = null;
                return false;
            }

            var stringTypeMapping = ExpressionExtensions.InferTypeMapping(translatedInstance, translatedPattern);

            translatedInstance = _sqlExpressionFactory.ApplyTypeMapping(translatedInstance, stringTypeMapping);
            translatedPattern = _sqlExpressionFactory.ApplyTypeMapping(translatedPattern, stringTypeMapping);

            switch (translatedPattern)
            {
                case SqlConstantExpression patternConstant:
                {
                    translation = patternConstant.Value switch
                    {
                        null => _sqlExpressionFactory.Like(
                            translatedInstance,
                            _sqlExpressionFactory.Constant(null, typeof(string), stringTypeMapping)),

                        "" => _sqlExpressionFactory.Like(translatedInstance, _sqlExpressionFactory.Constant("%")),

                        string s when !s.Any(IsLikeWildChar)
                            => _sqlExpressionFactory.Like(
                                translatedInstance,
                                _sqlExpressionFactory.Constant(
                                    methodType switch
                                    {
                                        StartsEndsWithContains.StartsWith => s + '%',
                                        StartsEndsWithContains.EndsWith => '%' + s,
                                        StartsEndsWithContains.Contains => $"%{s}%",

                                        _ => throw new ArgumentOutOfRangeException(nameof(methodType), methodType, null)
                                    })),

                        string s => _sqlExpressionFactory.Like(
                            translatedInstance,
                            _sqlExpressionFactory.Constant(
                                methodType switch
                                {
                                    StartsEndsWithContains.StartsWith => EscapeLikePattern(s) + '%',
                                    StartsEndsWithContains.EndsWith => '%' + EscapeLikePattern(s),
                                    StartsEndsWithContains.Contains => $"%{EscapeLikePattern(s)}%",

                                    _ => throw new ArgumentOutOfRangeException(nameof(methodType), methodType, null)
                                }),
                            _sqlExpressionFactory.Constant(LikeEscapeString)),

                        char s when !IsLikeWildChar(s)
                            => _sqlExpressionFactory.Like(
                                translatedInstance,
                                _sqlExpressionFactory.Constant(
                                    methodType switch
                                    {
                                        StartsEndsWithContains.StartsWith => s + "%",
                                        StartsEndsWithContains.EndsWith => "%" + s,
                                        StartsEndsWithContains.Contains => $"%{s}%",

                                        _ => throw new ArgumentOutOfRangeException(nameof(methodType), methodType, null)
                                    })),

                        char s => _sqlExpressionFactory.Like(
                            translatedInstance,
                            _sqlExpressionFactory.Constant(
                                methodType switch
                                {
                                    StartsEndsWithContains.StartsWith => LikeEscapeChar + s + "%",
                                    StartsEndsWithContains.EndsWith => "%" + LikeEscapeChar + s,
                                    StartsEndsWithContains.Contains => $"%{LikeEscapeChar}{s}%",

                                    _ => throw new ArgumentOutOfRangeException(nameof(methodType), methodType, null)
                                }),
                            _sqlExpressionFactory.Constant(LikeEscapeString)),

                        _ => throw new UnreachableException()
                    };

                    return true;
                }

                case SqlParameterExpression patternParameter:
                {
                    var lambda = Expression.Lambda(
                        Expression.Call(
                            EscapeLikePatternParameterMethod,
                            QueryCompilationContext.QueryContextParameter,
                            Expression.Constant(patternParameter.Name),
                            Expression.Constant(methodType)),
                        QueryCompilationContext.QueryContextParameter);

                    var escapedPatternParameter =
                        _queryCompilationContext.RegisterRuntimeParameter(
                            $"{patternParameter.Name}_{methodType.ToString().ToLower(CultureInfo.InvariantCulture)}", lambda);

                    translation = _sqlExpressionFactory.Like(
                        translatedInstance,
                        new SqlParameterExpression(escapedPatternParameter.Name!, escapedPatternParameter.Type, stringTypeMapping),
                        _sqlExpressionFactory.Constant(LikeEscapeString));

                    return true;
                }

                default:
                    translation = TranslateWithoutLike();
                    return true;
            }

            SqlExpression TranslateWithoutLike(bool patternIsNonEmptyConstantString = false)
            {
                return methodType switch
                {
                    StartsEndsWithContains.StartsWith or StartsEndsWithContains.EndsWith
                        => _sqlExpressionFactory.AndAlso(
                            _sqlExpressionFactory.IsNotNull(translatedInstance),
                            _sqlExpressionFactory.AndAlso(
                                _sqlExpressionFactory.IsNotNull(translatedPattern),
                                _sqlExpressionFactory.Equal(
                                    _sqlExpressionFactory.Function(
                                        methodType is StartsEndsWithContains.StartsWith ? "LEFT" : "RIGHT",
                                        [
                                            translatedInstance,
                                            _sqlExpressionFactory.Function(
                                                "LEN",
                                                [translatedPattern],
                                                nullable: true,
                                                argumentsPropagateNullability: Statics.TrueArrays[1],
                                                typeof(int))
                                        ],
                                        nullable: true,
                                        argumentsPropagateNullability: Statics.TrueArrays[2],
                                        typeof(string),
                                        stringTypeMapping),
                                    translatedPattern))),

                    StartsEndsWithContains.Contains when patternIsNonEmptyConstantString
                        => _sqlExpressionFactory.AndAlso(
                            _sqlExpressionFactory.IsNotNull(translatedInstance),
                            CharIndexGreaterThanZero()),

                    StartsEndsWithContains.Contains
                        => _sqlExpressionFactory.AndAlso(
                            _sqlExpressionFactory.IsNotNull(translatedInstance),
                            _sqlExpressionFactory.AndAlso(
                                _sqlExpressionFactory.IsNotNull(translatedPattern),
                                _sqlExpressionFactory.OrElse(
                                    CharIndexGreaterThanZero(),
                                    _sqlExpressionFactory.Like(
                                        translatedPattern,
                                        _sqlExpressionFactory.Constant(string.Empty, stringTypeMapping))))),

                    _ => throw new UnreachableException()
                };

                SqlExpression CharIndexGreaterThanZero()
                    => _sqlExpressionFactory.GreaterThan(
                        _sqlExpressionFactory.Function(
                            "CHARINDEX",
                            [translatedPattern, translatedInstance],
                            nullable: true,
                            argumentsPropagateNullability: Statics.TrueArrays[2],
                            typeof(int)),
                        _sqlExpressionFactory.Constant(0));
            }
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    [EntityFrameworkInternal]
    public static string? ConstructLikePatternParameter(
        QueryContext queryContext,
        string baseParameterName,
        StartsEndsWithContains methodType)
        => queryContext.Parameters[baseParameterName] switch
        {
            null => null,
            "" => "%",

            string s => methodType switch
            {
                StartsEndsWithContains.StartsWith => EscapeLikePattern(s) + '%',
                StartsEndsWithContains.EndsWith => '%' + EscapeLikePattern(s),
                StartsEndsWithContains.Contains => $"%{EscapeLikePattern(s)}%",
                _ => throw new ArgumentOutOfRangeException(nameof(methodType), methodType, null)
            },

            char s when !IsLikeWildChar(s) => methodType switch
            {
                StartsEndsWithContains.StartsWith => s + "%",
                StartsEndsWithContains.EndsWith => "%" + s,
                StartsEndsWithContains.Contains => $"%{s}%",
                _ => throw new ArgumentOutOfRangeException(nameof(methodType), methodType, null)
            },

            char s => methodType switch
            {
                StartsEndsWithContains.StartsWith => LikeEscapeChar + s + "%",
                StartsEndsWithContains.EndsWith => "%" + LikeEscapeChar + s,
                StartsEndsWithContains.Contains => $"%{LikeEscapeChar}{s}%",
                _ => throw new ArgumentOutOfRangeException(nameof(methodType), methodType, null)
            },

            _ => throw new UnreachableException()
        };

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    [EntityFrameworkInternal]
    public enum StartsEndsWithContains
    {
        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        StartsWith,

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        EndsWith,

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        Contains
    }

    private static bool IsLikeWildChar(char c)
        => c is '%' or '_' or '[';

    private static string EscapeLikePattern(string pattern)
    {
        int i;
        for (i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (IsLikeWildChar(c) || c == LikeEscapeChar)
            {
                break;
            }
        }

        if (i == pattern.Length)
        {
            return pattern;
        }

        var builder = new StringBuilder(pattern, 0, i, pattern.Length + 10);

        for (; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (IsLikeWildChar(c)
                || c == LikeEscapeChar)
            {
                builder.Append(LikeEscapeChar);
            }

            builder.Append(c);
        }

        return builder.ToString();
    }

    // VistaDB: no analog — SqlServer overrides GenerateGreatest/GenerateLeast to emit the GREATEST/LEAST
    // built-ins (SqlServer 2025+ / compat 160+). VistaDB has no GREATEST/LEAST; we let the relational base
    // fall back to CASE WHEN expansion.
    // Original SqlServer logic preserved below for future revival when VistaDB adds GREATEST/LEAST.
    /*
        public override SqlExpression? GenerateGreatest(IReadOnlyList<SqlExpression> expressions, Type resultType)
        {
            // ... emits GREATEST(...) when compat level ≥ 160.
        }

        public override SqlExpression? GenerateLeast(IReadOnlyList<SqlExpression> expressions, Type resultType)
        {
            // ... emits LEAST(...) when compat level ≥ 160.
        }
    */

    private Expression TranslateByteArrayElementAccess(Expression array, Expression index, Type resultType)
    {
        var visitedArray = Visit(array);
        var visitedIndex = Visit(index);

        if (visitedArray is not SqlExpression sqlArray
            || visitedIndex is not SqlExpression sqlIndex)
        {
            return QueryCompilationContext.NotTranslatedExpression;
        }

        // VistaDB difference from SqlServer: SUBSTRING on a varbinary column returns a single character
        // (nchar/varchar) rather than binary, so CAST(SUBSTRING([Photo],1,1) AS tinyint) fails with
        // "cannot convert 'e' to byte" for byte value 0x65 = 101. Wrap in UNICODE() to get the
        // integer code-point of that character, which equals the byte value for all bytes 0–255.
        var substringExpr = Dependencies.SqlExpressionFactory.Function(
            "SUBSTRING",
            [
                sqlArray,
                Dependencies.SqlExpressionFactory.Add(
                    Dependencies.SqlExpressionFactory.ApplyDefaultTypeMapping(sqlIndex),
                    Dependencies.SqlExpressionFactory.Constant(1)),
                Dependencies.SqlExpressionFactory.Constant(1)
            ],
            nullable: true,
            argumentsPropagateNullability: Statics.TrueArrays[3],
            typeof(string));

        return Dependencies.SqlExpressionFactory.Function(
            "UNICODE",
            [substringExpr],
            nullable: true,
            argumentsPropagateNullability: Statics.TrueArrays[1],
            // Expose as int — the caller (IsIdentityOperation, etc.) only cares that it's a scalar.
            // For e.Photo[0] == 101 comparisons, both sides will be int and EF Core handles that.
            typeof(int));
    }

    private static string? GetProviderType(SqlExpression expression)
        => expression.TypeMapping?.StoreType;
}
