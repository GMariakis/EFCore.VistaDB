// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using ExpressionExtensions = Microsoft.EntityFrameworkCore.Query.ExpressionExtensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore.VistaDB.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBMathTranslator : IMethodCallTranslator
{
    // VistaDB supports ABS, CEILING, FLOOR, ROUND, SIGN, SQRT, POWER, LOG, EXP. The remaining T-SQL
    // numerics (ACOS/ASIN/ATAN/ATN2/COS/SIN/TAN/RADIANS/DEGREES/LOG10) are documented as supported by
    // the engine; if a runtime "no such function" error surfaces, prune the entry from this table.
    private static readonly Dictionary<MethodInfo, string> SupportedMethodTranslations = new()
    {
        { typeof(Math).GetRuntimeMethod(nameof(Math.Abs), [typeof(decimal)])!, "ABS" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Abs), [typeof(double)])!, "ABS" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Abs), [typeof(float)])!, "ABS" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Abs), [typeof(int)])!, "ABS" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Abs), [typeof(long)])!, "ABS" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Abs), [typeof(sbyte)])!, "ABS" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Abs), [typeof(short)])!, "ABS" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Ceiling), [typeof(decimal)])!, "CEILING" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Ceiling), [typeof(double)])!, "CEILING" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Floor), [typeof(decimal)])!, "FLOOR" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Floor), [typeof(double)])!, "FLOOR" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Pow), [typeof(double), typeof(double)])!, "POWER" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Exp), [typeof(double)])!, "EXP" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Log), [typeof(double)])!, "LOG" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Log), [typeof(double), typeof(double)])!, "LOG" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Sqrt), [typeof(double)])!, "SQRT" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Sign), [typeof(decimal)])!, "SIGN" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Sign), [typeof(double)])!, "SIGN" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Sign), [typeof(float)])!, "SIGN" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Sign), [typeof(int)])!, "SIGN" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Sign), [typeof(long)])!, "SIGN" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Sign), [typeof(sbyte)])!, "SIGN" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Sign), [typeof(short)])!, "SIGN" },
        { typeof(MathF).GetRuntimeMethod(nameof(MathF.Ceiling), [typeof(float)])!, "CEILING" },
        { typeof(MathF).GetRuntimeMethod(nameof(MathF.Floor), [typeof(float)])!, "FLOOR" },
        { typeof(MathF).GetRuntimeMethod(nameof(MathF.Pow), [typeof(float), typeof(float)])!, "POWER" },
        { typeof(MathF).GetRuntimeMethod(nameof(MathF.Exp), [typeof(float)])!, "EXP" },
        { typeof(MathF).GetRuntimeMethod(nameof(MathF.Log), [typeof(float)])!, "LOG" },
        { typeof(MathF).GetRuntimeMethod(nameof(MathF.Log), [typeof(float), typeof(float)])!, "LOG" },
        { typeof(MathF).GetRuntimeMethod(nameof(MathF.Sqrt), [typeof(float)])!, "SQRT" }
    };

    // VistaDB: no analog — SqlServer additionally maps Acos/Asin/Atan/Atan2/Cos/Sin/Tan/Log10/Degrees/Radians,
    // since not all are documented as VistaDB built-ins. The relational base does not register these, so we
    // simply leave them untranslated (a clear translation failure if a user hits them).
    // Original SqlServer entries preserved below for future revival.
    /*
        { typeof(Math).GetRuntimeMethod(nameof(Math.Log10), [typeof(double)])!, "LOG10" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Acos), [typeof(double)])!, "ACOS" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Asin), [typeof(double)])!, "ASIN" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Atan), [typeof(double)])!, "ATAN" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Atan2), [typeof(double), typeof(double)])!, "ATN2" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Cos), [typeof(double)])!, "COS" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Sin), [typeof(double)])!, "SIN" },
        { typeof(Math).GetRuntimeMethod(nameof(Math.Tan), [typeof(double)])!, "TAN" },
        { typeof(double).GetRuntimeMethod(nameof(double.DegreesToRadians), [typeof(double)])!, "RADIANS" },
        { typeof(double).GetRuntimeMethod(nameof(double.RadiansToDegrees), [typeof(double)])!, "DEGREES" },
        ... and the MathF mirrors.
    */

    private static readonly IEnumerable<MethodInfo> TruncateMethodInfos =
    [
        typeof(Math).GetRuntimeMethod(nameof(Math.Truncate), [typeof(decimal)])!,
        typeof(Math).GetRuntimeMethod(nameof(Math.Truncate), [typeof(double)])!,
        typeof(MathF).GetRuntimeMethod(nameof(MathF.Truncate), [typeof(float)])!
    ];

    private static readonly IEnumerable<MethodInfo> RoundMethodInfos =
    [
        typeof(Math).GetRuntimeMethod(nameof(Math.Round), [typeof(decimal)])!,
        typeof(Math).GetRuntimeMethod(nameof(Math.Round), [typeof(double)])!,
        typeof(Math).GetRuntimeMethod(nameof(Math.Round), [typeof(decimal), typeof(int)])!,
        typeof(Math).GetRuntimeMethod(nameof(Math.Round), [typeof(double), typeof(int)])!,
        typeof(MathF).GetRuntimeMethod(nameof(MathF.Round), [typeof(float)])!,
        typeof(MathF).GetRuntimeMethod(nameof(MathF.Round), [typeof(float), typeof(int)])!
    ];

    private readonly ISqlExpressionFactory _sqlExpressionFactory;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBMathTranslator(ISqlExpressionFactory sqlExpressionFactory)
        => _sqlExpressionFactory = sqlExpressionFactory;

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
        if (SupportedMethodTranslations.TryGetValue(method, out var sqlFunctionName))
        {
            var typeMapping = arguments.Count == 1
                ? ExpressionExtensions.InferTypeMapping(arguments[0])
                : ExpressionExtensions.InferTypeMapping(arguments[0], arguments[1]);

            var newArguments = new SqlExpression[arguments.Count];
            newArguments[0] = _sqlExpressionFactory.ApplyTypeMapping(arguments[0], typeMapping);

            if (arguments.Count == 2)
            {
                newArguments[1] = _sqlExpressionFactory.ApplyTypeMapping(arguments[1], typeMapping);
            }

            return _sqlExpressionFactory.Function(
                sqlFunctionName,
                newArguments,
                nullable: true,
                argumentsPropagateNullability: newArguments.Select(_ => true).ToArray(),
                method.ReturnType,
                sqlFunctionName == "SIGN" ? null : typeMapping);
        }

        if (TruncateMethodInfos.Contains(method))
        {
            var argument = arguments[0];
            var resultType = argument.Type;
            if (resultType == typeof(float))
            {
                resultType = typeof(double);
            }

            var result = _sqlExpressionFactory.Function(
                "ROUND",
                [argument, _sqlExpressionFactory.Constant(0), _sqlExpressionFactory.Constant(1)],
                nullable: true,
                argumentsPropagateNullability: [true, false, false],
                resultType);

            if (argument.Type == typeof(float))
            {
                result = _sqlExpressionFactory.Convert(result, typeof(float));
            }

            return _sqlExpressionFactory.ApplyTypeMapping(result, argument.TypeMapping);
        }

        if (RoundMethodInfos.Contains(method))
        {
            var argument = arguments[0];
            var digits = arguments.Count == 2 ? arguments[1] : _sqlExpressionFactory.Constant(0);
            var resultType = argument.Type;
            if (resultType == typeof(float))
            {
                resultType = typeof(double);
            }

            var result = _sqlExpressionFactory.Function(
                "ROUND",
                [argument, digits],
                nullable: true,
                argumentsPropagateNullability: Statics.TrueArrays[2],
                resultType);

            if (argument.Type == typeof(float))
            {
                result = _sqlExpressionFactory.Convert(result, typeof(float));
            }

            return _sqlExpressionFactory.ApplyTypeMapping(result, argument.TypeMapping);
        }

        return null;
    }
}
