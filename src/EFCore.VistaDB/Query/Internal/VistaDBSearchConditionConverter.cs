// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Microsoft.EntityFrameworkCore.VistaDB.Query.Internal;

/// <summary>
///     <para>
///         Converts boolean expressions between search-condition form and value form depending on where
///         they appear, because VistaDB — like SQL Server — has no true boolean type and will not accept a
///         bare <c>bit</c> column where a condition belongs:
///     </para>
///     <code>
///         WHERE b.SomeBitColumn      =&gt; WHERE b.SomeBitColumn = CAST(1 AS bit)
///         SELECT a LIKE b            =&gt; SELECT CASE WHEN a LIKE b THEN 1 ELSE 0 END
///     </code>
///     <para>
///         Without this pass VistaDB raises error 666, "a column is referenced in a context where a boolean
///         condition is expected". Writing the comparison out in LINQ does not help: EF collapses
///         <c>x == true</c> back to the bare column, which is why the fix has to run here, after all other
///         processing has finished.
///     </para>
/// </summary>
/// <remarks>
///     <para>
///         Ported from the SQL Server provider's <c>SearchConditionConverter</c>. Two of its optimisations
///         are deliberately left out: rewriting <c>a != b</c> to <c>a ^ b</c>, and <c>NOT a</c> to
///         <c>~a</c>. Both emit T-SQL bitwise operators whose VistaDB support is unverified; without them
///         those cases fall through to the portable <c>CASE WHEN</c> form — wordier SQL, same results.
///     </para>
///     <para>
///         This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///         the same compatibility standards as public APIs. It may be changed or removed without notice in
///         any release.
///     </para>
/// </remarks>
public class VistaDBSearchConditionConverter(ISqlExpressionFactory sqlExpressionFactory) : ExpressionVisitor
{
    /// <inheritdoc />
    [return: NotNullIfNotNull(nameof(expression))]
    public override Expression? Visit(Expression? expression)
        => Visit(expression, inSearchConditionContext: false, allowNullFalseEquivalence: false);

    /// <summary>
    ///     Visits <paramref name="expression" /> knowing whether the position it occupies wants a condition
    ///     (a <c>WHERE</c>, a <c>JOIN ... ON</c>, a <c>CASE WHEN</c> test) or a value (a projection, an
    ///     ordering, an operand).
    /// </summary>
    [return: NotNullIfNotNull(nameof(expression))]
    protected virtual Expression? Visit(Expression? expression, bool inSearchConditionContext, bool allowNullFalseEquivalence)
        => expression switch
        {
            CaseExpression e => VisitCase(e, inSearchConditionContext, allowNullFalseEquivalence),
            SelectExpression e => VisitSelect(e),
            SqlBinaryExpression e => VisitSqlBinary(e, inSearchConditionContext, allowNullFalseEquivalence),
            SqlUnaryExpression e => VisitSqlUnary(e, inSearchConditionContext),
            PredicateJoinExpressionBase e => VisitPredicateJoin(e),

            // These are search conditions in their own right: they belong in a WHERE and cannot be
            // projected out directly.
            SqlExpression e and (ExistsExpression or InExpression or LikeExpression)
                => ApplyConversion((SqlExpression)base.VisitExtension(e), inSearchConditionContext, isExpressionSearchCondition: true),

            SqlExpression e
                => ApplyConversion((SqlExpression)base.VisitExtension(e), inSearchConditionContext, isExpressionSearchCondition: false),

            _ => base.Visit(expression)
        };

    /// <summary>Bridges the gap between what an expression is and what its position requires.</summary>
    private SqlExpression ApplyConversion(SqlExpression sqlExpression, bool inSearchConditionContext, bool isExpressionSearchCondition)
        => (inSearchCondition: inSearchConditionContext, isExpressionSearchCondition) switch
        {
            // A value where a condition is expected — compare it. This is the whole point of the pass.
            // WHERE b.SomeBitColumn => WHERE b.SomeBitColumn = CAST(1 AS bit)
            (true, false) => sqlExpression is SqlConstantExpression { Value: bool boolValue }
                // A bare literal cannot be compared to itself, so express it as an integer comparison
                // that is trivially true or false.
                ? sqlExpressionFactory.Equal(
                    sqlExpressionFactory.Constant(boolValue ? 1 : 0),
                    sqlExpressionFactory.Constant(1))
                : sqlExpressionFactory.Equal(sqlExpression, sqlExpressionFactory.Constant(true)),

            // A condition where a value is expected — wrap it so it can be projected or ordered by.
            // SELECT a LIKE b => SELECT CASE WHEN a LIKE b THEN 1 ELSE 0 END
            (false, true) => sqlExpressionFactory.Case(
                [
                    new CaseWhenClause(
                        SimplifyNegatedBinary(sqlExpression),
                        sqlExpressionFactory.ApplyDefaultTypeMapping(sqlExpressionFactory.Constant(true)))
                ],
                sqlExpressionFactory.Constant(false)),

            // Already in the right form: WHERE a LIKE b, SELECT b.SomeBitColumn.
            _ => sqlExpression
        };

    /// <summary>
    ///     Folds <c>NOT (a = b)</c> into <c>a &lt;&gt; b</c> and <c>NOT (a = true)</c> into <c>a = false</c>,
    ///     so a redundant negation is not carried into a CASE test.
    /// </summary>
    private SqlExpression SimplifyNegatedBinary(SqlExpression sqlExpression)
    {
        if (sqlExpression is SqlUnaryExpression { OperatorType: ExpressionType.Not } sqlUnaryExpression
            && sqlUnaryExpression.Type == typeof(bool)
            && sqlUnaryExpression.Operand is SqlBinaryExpression { OperatorType: ExpressionType.Equal } sqlBinaryOperand)
        {
            if (sqlBinaryOperand.Left.Type == typeof(bool)
                && sqlBinaryOperand.Right.Type == typeof(bool)
                && (sqlBinaryOperand.Left is SqlConstantExpression || sqlBinaryOperand.Right is SqlConstantExpression))
            {
                var constant = sqlBinaryOperand.Left as SqlConstantExpression ?? (SqlConstantExpression)sqlBinaryOperand.Right;
                if (sqlBinaryOperand.Left is SqlConstantExpression)
                {
                    return sqlExpressionFactory.MakeBinary(
                        ExpressionType.Equal,
                        sqlExpressionFactory.Constant(!(bool)constant.Value!, constant.TypeMapping),
                        sqlBinaryOperand.Right,
                        sqlBinaryOperand.TypeMapping)!;
                }

                return sqlExpressionFactory.MakeBinary(
                    ExpressionType.Equal,
                    sqlBinaryOperand.Left,
                    sqlExpressionFactory.Constant(!(bool)constant.Value!, constant.TypeMapping),
                    sqlBinaryOperand.TypeMapping)!;
            }

            return sqlExpressionFactory.MakeBinary(
                ExpressionType.NotEqual,
                sqlBinaryOperand.Left,
                sqlBinaryOperand.Right,
                sqlBinaryOperand.TypeMapping)!;
        }

        return sqlExpression;
    }

    /// <summary>
    ///     A <c>CASE</c> with no operand tests conditions; one with an operand tests values. Results are
    ///     always values.
    /// </summary>
    protected virtual Expression VisitCase(CaseExpression caseExpression, bool inSearchConditionContext, bool allowNullFalseEquivalence)
    {
        var testIsCondition = caseExpression.Operand is null;
        var operand = (SqlExpression?)Visit(caseExpression.Operand);
        var whenClauses = new List<CaseWhenClause>();
        foreach (var whenClause in caseExpression.WhenClauses)
        {
            var test = (SqlExpression)Visit(whenClause.Test, testIsCondition, testIsCondition);
            var result = (SqlExpression)Visit(whenClause.Result, inSearchConditionContext: false, allowNullFalseEquivalence);
            whenClauses.Add(new CaseWhenClause(test, result));
        }

        var elseResult = (SqlExpression?)Visit(caseExpression.ElseResult, inSearchConditionContext: false, allowNullFalseEquivalence);

        return ApplyConversion(
            sqlExpressionFactory.Case(operand, whenClauses, elseResult, caseExpression),
            inSearchConditionContext,
            isExpressionSearchCondition: false);
    }

    /// <summary>A join predicate is always a condition.</summary>
    protected virtual Expression VisitPredicateJoin(PredicateJoinExpressionBase join)
        => join.Update(
            (TableExpressionBase)Visit(join.Table),
            (SqlExpression)Visit(join.JoinPredicate, inSearchConditionContext: true, allowNullFalseEquivalence: true));

    /// <summary>
    ///     WHERE and HAVING want conditions; projections, orderings, GROUP BY, OFFSET and LIMIT want values.
    /// </summary>
    protected virtual Expression VisitSelect(SelectExpression select)
    {
        var tables = this.VisitAndConvert(select.Tables);
        var predicate = (SqlExpression?)Visit(select.Predicate, inSearchConditionContext: true, allowNullFalseEquivalence: true);
        var groupBy = this.VisitAndConvert(select.GroupBy);
        var having = (SqlExpression?)Visit(select.Having, inSearchConditionContext: true, allowNullFalseEquivalence: true);
        var projections = this.VisitAndConvert(select.Projection);
        var orderings = this.VisitAndConvert(select.Orderings);
        var offset = (SqlExpression?)Visit(select.Offset);
        var limit = (SqlExpression?)Visit(select.Limit);

        return select.Update(tables, predicate, groupBy, having, projections, orderings, offset, limit);
    }

    /// <summary>
    ///     Only <c>AND</c>/<c>OR</c> want conditions on both sides; comparison operands are values.
    /// </summary>
    protected virtual Expression VisitSqlBinary(SqlBinaryExpression binary, bool inSearchConditionContext, bool allowNullFalseEquivalence)
    {
        var areOperandsInSearchConditionContext = binary.OperatorType is ExpressionType.AndAlso or ExpressionType.OrElse;

        var newLeft = (SqlExpression)Visit(binary.Left, areOperandsInSearchConditionContext, allowNullFalseEquivalence: false);
        var newRight = (SqlExpression)Visit(binary.Right, areOperandsInSearchConditionContext, allowNullFalseEquivalence: false);

        binary = binary.Update(newLeft, newRight);

        var isExpressionSearchCondition = binary.OperatorType is ExpressionType.AndAlso
            or ExpressionType.OrElse
            or ExpressionType.Equal
            or ExpressionType.NotEqual
            or ExpressionType.GreaterThan
            or ExpressionType.GreaterThanOrEqual
            or ExpressionType.LessThan
            or ExpressionType.LessThanOrEqual;

        return ApplyConversion(binary, inSearchConditionContext, isExpressionSearchCondition);
    }

    /// <summary>
    ///     <c>NOT</c> over a boolean and the null checks yield conditions; casts and arithmetic negation
    ///     yield values.
    /// </summary>
    protected virtual Expression VisitSqlUnary(SqlUnaryExpression sqlUnaryExpression, bool inSearchConditionContext)
    {
        bool isOperandInSearchConditionContext, isSearchConditionExpression;

        switch (sqlUnaryExpression.OperatorType)
        {
            // Logical NOT. SQL Server sometimes emits "~x" here to stay in value form; VistaDB always goes
            // through the predicate form, and ApplyConversion wraps it in CASE WHEN when the position
            // wants a value.
            case ExpressionType.Not
                when (sqlUnaryExpression.TypeMapping?.Converter?.ProviderClrType ?? sqlUnaryExpression.Type) == typeof(bool):
                isOperandInSearchConditionContext = true;
                isSearchConditionExpression = true;
                break;

            // Bitwise NOT, casts and arithmetic negation are value-to-value.
            case ExpressionType.Not:
            case ExpressionType.Convert:
            case ExpressionType.Negate:
                isOperandInSearchConditionContext = false;
                isSearchConditionExpression = false;
                break;

            // IS NULL / IS NOT NULL take a value and yield a condition.
            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
                isOperandInSearchConditionContext = false;
                isSearchConditionExpression = true;
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported operator '{sqlUnaryExpression.OperatorType}' on {nameof(SqlUnaryExpression)}.");
        }

        var operand = (SqlExpression)Visit(sqlUnaryExpression.Operand, isOperandInSearchConditionContext, allowNullFalseEquivalence: false);

        return SimplifyNegatedBinary(
            ApplyConversion(
                sqlUnaryExpression.Update(operand),
                inSearchConditionContext,
                isSearchConditionExpression));
    }
}
