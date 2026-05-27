// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using CharTypeMapping = Microsoft.EntityFrameworkCore.Storage.CharTypeMapping;
using ExpressionExtensions = Microsoft.EntityFrameworkCore.Query.ExpressionExtensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore.VistaDB.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBStringMethodTranslator : IMethodCallTranslator
{
    private static readonly MethodInfo IndexOfMethodInfoString
        = typeof(string).GetRuntimeMethod(nameof(string.IndexOf), [typeof(string)])!;

    private static readonly MethodInfo IndexOfMethodInfoChar
        = typeof(string).GetRuntimeMethod(nameof(string.IndexOf), [typeof(char)])!;

    private static readonly MethodInfo IndexOfMethodInfoWithStartingPositionString
        = typeof(string).GetRuntimeMethod(nameof(string.IndexOf), [typeof(string), typeof(int)])!;

    private static readonly MethodInfo IndexOfMethodInfoWithStartingPositionChar
        = typeof(string).GetRuntimeMethod(nameof(string.IndexOf), [typeof(char), typeof(int)])!;

    private static readonly MethodInfo ReplaceMethodInfoString
        = typeof(string).GetRuntimeMethod(nameof(string.Replace), [typeof(string), typeof(string)])!;

    private static readonly MethodInfo ReplaceMethodInfoChar
        = typeof(string).GetRuntimeMethod(nameof(string.Replace), [typeof(char), typeof(char)])!;

    private static readonly MethodInfo ToLowerMethodInfo
        = typeof(string).GetRuntimeMethod(nameof(string.ToLower), Type.EmptyTypes)!;

    private static readonly MethodInfo ToUpperMethodInfo
        = typeof(string).GetRuntimeMethod(nameof(string.ToUpper), Type.EmptyTypes)!;

    private static readonly MethodInfo SubstringMethodInfoWithOneArg
        = typeof(string).GetRuntimeMethod(nameof(string.Substring), [typeof(int)])!;

    private static readonly MethodInfo SubstringMethodInfoWithTwoArgs
        = typeof(string).GetRuntimeMethod(nameof(string.Substring), [typeof(int), typeof(int)])!;

    private static readonly MethodInfo IsNullOrEmptyMethodInfo
        = typeof(string).GetRuntimeMethod(nameof(string.IsNullOrEmpty), [typeof(string)])!;

    private static readonly MethodInfo IsNullOrWhiteSpaceMethodInfo
        = typeof(string).GetRuntimeMethod(nameof(string.IsNullOrWhiteSpace), [typeof(string)])!;

    private static readonly MethodInfo TrimStartMethodInfoWithoutArgs
        = typeof(string).GetRuntimeMethod(nameof(string.TrimStart), Type.EmptyTypes)!;

    private static readonly MethodInfo TrimEndMethodInfoWithoutArgs
        = typeof(string).GetRuntimeMethod(nameof(string.TrimEnd), Type.EmptyTypes)!;

    private static readonly MethodInfo TrimMethodInfoWithoutArgs
        = typeof(string).GetRuntimeMethod(nameof(string.Trim), Type.EmptyTypes)!;

    private static readonly MethodInfo TrimStartMethodInfoWithCharArrayArg
        = typeof(string).GetRuntimeMethod(nameof(string.TrimStart), [typeof(char[])])!;

    private static readonly MethodInfo TrimEndMethodInfoWithCharArrayArg
        = typeof(string).GetRuntimeMethod(nameof(string.TrimEnd), [typeof(char[])])!;

    private static readonly MethodInfo TrimMethodInfoWithCharArrayArg
        = typeof(string).GetRuntimeMethod(nameof(string.Trim), [typeof(char[])])!;

    private static readonly MethodInfo FirstOrDefaultMethodInfoWithoutArgs
        = typeof(Enumerable).GetRuntimeMethods().Single(m => m.Name == nameof(Enumerable.FirstOrDefault)
            && m.GetParameters().Length == 1).MakeGenericMethod(typeof(char));

    private static readonly MethodInfo LastOrDefaultMethodInfoWithoutArgs
        = typeof(Enumerable).GetRuntimeMethods().Single(m => m.Name == nameof(Enumerable.LastOrDefault)
            && m.GetParameters().Length == 1).MakeGenericMethod(typeof(char));

    // VistaDB: no analog — SqlServer also accepts char-arg TrimStart/TrimEnd overloads when compatibility
    // level >= 160 (SQL Server 2022+ LTRIM(string, chars)). VistaDB has no equivalent. We only translate
    // the no-arg and empty-char-array overloads.
    // Original SqlServer SqlServerDbFunctionsExtensions.PatIndex translator is also intentionally omitted —
    // VistaDB has no PATINDEX surface in EF.Functions yet.
    // Original SqlServer state preserved below for future revival.
    /*
        private static readonly MethodInfo TrimStartMethodInfoWithCharArg = typeof(string).GetRuntimeMethod(nameof(string.TrimStart), [typeof(char)])!;
        private static readonly MethodInfo TrimEndMethodInfoWithCharArg   = typeof(string).GetRuntimeMethod(nameof(string.TrimEnd),   [typeof(char)])!;
        private static readonly MethodInfo PatIndexMethodInfo = typeof(SqlServerDbFunctionsExtensions).GetRuntimeMethod(
            nameof(SqlServerDbFunctionsExtensions.PatIndex), [typeof(DbFunctions), typeof(string), typeof(string)])!;
    */

    private readonly ISqlExpressionFactory _sqlExpressionFactory;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBStringMethodTranslator(ISqlExpressionFactory sqlExpressionFactory)
    {
        _sqlExpressionFactory = sqlExpressionFactory;
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
        if (instance != null)
        {
            if (IndexOfMethodInfoString.Equals(method) || IndexOfMethodInfoChar.Equals(method))
            {
                return TranslateIndexOf(instance, method, arguments[0], null);
            }

            if (IndexOfMethodInfoWithStartingPositionString.Equals(method) || IndexOfMethodInfoWithStartingPositionChar.Equals(method))
            {
                return TranslateIndexOf(instance, method, arguments[0], arguments[1]);
            }

            if (ReplaceMethodInfoString.Equals(method) || ReplaceMethodInfoChar.Equals(method))
            {
                var firstArgument = arguments[0];
                var secondArgument = arguments[1];
                var stringTypeMapping = ExpressionExtensions.InferTypeMapping(instance, firstArgument, secondArgument);

                instance = _sqlExpressionFactory.ApplyTypeMapping(instance, stringTypeMapping);
                firstArgument = _sqlExpressionFactory.ApplyTypeMapping(
                    firstArgument, firstArgument.Type == typeof(char) ? CharTypeMapping.Default : stringTypeMapping);
                secondArgument = _sqlExpressionFactory.ApplyTypeMapping(
                    secondArgument, secondArgument.Type == typeof(char) ? CharTypeMapping.Default : stringTypeMapping);

                return _sqlExpressionFactory.Function(
                    "REPLACE",
                    [instance, firstArgument, secondArgument],
                    nullable: true,
                    argumentsPropagateNullability: Statics.TrueArrays[3],
                    method.ReturnType,
                    stringTypeMapping);
            }

            if (ToLowerMethodInfo.Equals(method)
                || ToUpperMethodInfo.Equals(method))
            {
                return _sqlExpressionFactory.Function(
                    ToLowerMethodInfo.Equals(method) ? "LOWER" : "UPPER",
                    [instance],
                    nullable: true,
                    argumentsPropagateNullability: Statics.TrueArrays[1],
                    method.ReturnType,
                    instance.TypeMapping);
            }

            if (SubstringMethodInfoWithOneArg.Equals(method))
            {
                return _sqlExpressionFactory.Function(
                    "SUBSTRING",
                    [
                        instance,
                        _sqlExpressionFactory.Add(
                            arguments[0],
                            _sqlExpressionFactory.Constant(1)),
                        _sqlExpressionFactory.Function(
                            "LEN",
                            [instance],
                            nullable: true,
                            argumentsPropagateNullability: Statics.TrueArrays[1],
                            typeof(int))
                    ],
                    nullable: true,
                    argumentsPropagateNullability: Statics.TrueArrays[3],
                    method.ReturnType,
                    instance.TypeMapping);
            }

            if (SubstringMethodInfoWithTwoArgs.Equals(method))
            {
                return _sqlExpressionFactory.Function(
                    "SUBSTRING",
                    [
                        instance,
                        _sqlExpressionFactory.Add(
                            arguments[0],
                            _sqlExpressionFactory.Constant(1)),
                        arguments[1]
                    ],
                    nullable: true,
                    argumentsPropagateNullability: Statics.TrueArrays[3],
                    method.ReturnType,
                    instance.TypeMapping);
            }

            // VistaDB supports the single-argument LTRIM/RTRIM (whitespace only).
            if (method == TrimStartMethodInfoWithoutArgs
                || (method == TrimStartMethodInfoWithCharArrayArg && arguments[0] is SqlConstantExpression { Value: char[] { Length: 0 } }))
            {
                return _sqlExpressionFactory.Function(
                    "LTRIM",
                    [instance],
                    nullable: true,
                    argumentsPropagateNullability: Statics.TrueArrays[1],
                    instance.Type,
                    instance.TypeMapping);
            }

            if (method == TrimEndMethodInfoWithoutArgs
                || (method == TrimEndMethodInfoWithCharArrayArg && arguments[0] is SqlConstantExpression { Value: char[] { Length: 0 } }))
            {
                return _sqlExpressionFactory.Function(
                    "RTRIM",
                    [instance],
                    nullable: true,
                    argumentsPropagateNullability: Statics.TrueArrays[1],
                    instance.Type,
                    instance.TypeMapping);
            }

            if (method == TrimMethodInfoWithoutArgs
                || (method == TrimMethodInfoWithCharArrayArg && arguments[0] is SqlConstantExpression { Value: char[] { Length: 0 } }))
            {
                return _sqlExpressionFactory.Function(
                    "LTRIM",
                    [
                        _sqlExpressionFactory.Function(
                            "RTRIM",
                            [instance],
                            nullable: true,
                            argumentsPropagateNullability: Statics.TrueArrays[1],
                            instance.Type,
                            instance.TypeMapping)
                    ],
                    nullable: true,
                    argumentsPropagateNullability: Statics.TrueArrays[1],
                    instance.Type,
                    instance.TypeMapping);
            }
        }

        if (IsNullOrEmptyMethodInfo.Equals(method))
        {
            var argument = arguments[0];

            return _sqlExpressionFactory.OrElse(
                _sqlExpressionFactory.IsNull(argument),
                _sqlExpressionFactory.Like(
                    argument,
                    _sqlExpressionFactory.Constant(string.Empty)));
        }

        if (IsNullOrWhiteSpaceMethodInfo.Equals(method))
        {
            var argument = arguments[0];

            return _sqlExpressionFactory.OrElse(
                _sqlExpressionFactory.IsNull(argument),
                _sqlExpressionFactory.Equal(
                    argument,
                    _sqlExpressionFactory.Constant(string.Empty, argument.TypeMapping)));
        }

        if (FirstOrDefaultMethodInfoWithoutArgs.Equals(method))
        {
            var argument = arguments[0];
            return _sqlExpressionFactory.Function(
                "SUBSTRING",
                [argument, _sqlExpressionFactory.Constant(1), _sqlExpressionFactory.Constant(1)],
                nullable: true,
                argumentsPropagateNullability: Statics.TrueArrays[3],
                method.ReturnType);
        }

        if (LastOrDefaultMethodInfoWithoutArgs.Equals(method))
        {
            var argument = arguments[0];
            return _sqlExpressionFactory.Function(
                "SUBSTRING",
                [
                    argument,
                    _sqlExpressionFactory.Function(
                        "LEN",
                        [argument],
                        nullable: true,
                        argumentsPropagateNullability: Statics.TrueArrays[1],
                        typeof(int)),
                    _sqlExpressionFactory.Constant(1)
                ],
                nullable: true,
                argumentsPropagateNullability: Statics.TrueArrays[3],
                method.ReturnType);
        }

        return null;
    }

    private SqlExpression TranslateIndexOf(
        SqlExpression instance,
        MethodInfo method,
        SqlExpression searchExpression,
        SqlExpression? startIndex)
    {
        var stringTypeMapping = ExpressionExtensions.InferTypeMapping(instance, searchExpression)!;
        searchExpression = _sqlExpressionFactory.ApplyTypeMapping(
            searchExpression, searchExpression.Type == typeof(char) ? CharTypeMapping.Default : stringTypeMapping);

        instance = _sqlExpressionFactory.ApplyTypeMapping(instance, stringTypeMapping);

        var charIndexArguments = new List<SqlExpression> { searchExpression, instance };

        if (startIndex is not null)
        {
            charIndexArguments.Add(
                startIndex is SqlConstantExpression { Value: int constantStartIndex }
                    ? _sqlExpressionFactory.Constant(constantStartIndex + 1, typeof(int))
                    : _sqlExpressionFactory.Add(startIndex, _sqlExpressionFactory.Constant(1)));
        }

        var argumentsPropagateNullability = Enumerable.Repeat(true, charIndexArguments.Count);

        var charIndexExpression = _sqlExpressionFactory.Function(
            "CHARINDEX",
            charIndexArguments,
            nullable: true,
            argumentsPropagateNullability,
            method.ReturnType);

        if (searchExpression is SqlConstantExpression { Value: "" })
        {
            return _sqlExpressionFactory.Case(
                [new CaseWhenClause(_sqlExpressionFactory.IsNotNull(instance), _sqlExpressionFactory.Constant(0))],
                elseResult: null
            );
        }

        var offsetExpression = searchExpression is SqlConstantExpression
            ? _sqlExpressionFactory.Constant(1)
            : _sqlExpressionFactory.Case(
                [
                    new CaseWhenClause(
                        _sqlExpressionFactory.Equal(
                            searchExpression,
                            _sqlExpressionFactory.Constant(string.Empty, stringTypeMapping)),
                        _sqlExpressionFactory.Constant(0))
                ],
                _sqlExpressionFactory.Constant(1));

        return _sqlExpressionFactory.Subtract(charIndexExpression, offsetExpression);
    }
}
