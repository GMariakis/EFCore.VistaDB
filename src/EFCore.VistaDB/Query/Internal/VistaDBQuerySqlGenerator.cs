// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.VistaDB.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDB.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBQuerySqlGenerator : QuerySqlGenerator
{
    private readonly IRelationalTypeMappingSource _typeMappingSource;
    private readonly ISqlGenerationHelper _sqlGenerationHelper;

    private bool _withinTable;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBQuerySqlGenerator(
        QuerySqlGeneratorDependencies dependencies,
        IRelationalTypeMappingSource typeMappingSource)
        : base(dependencies)
    {
        _typeMappingSource = typeMappingSource;
        _sqlGenerationHelper = dependencies.SqlGenerationHelper;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override bool TryGenerateWithoutWrappingSelect(SelectExpression selectExpression)
        // Match SqlServer: VALUES is not a top-level statement and must be wrapped in a SELECT.
        => selectExpression.Tables is not [ValuesExpression]
            && base.TryGenerateWithoutWrappingSelect(selectExpression);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitDelete(DeleteExpression deleteExpression)
    {
        var selectExpression = deleteExpression.SelectExpression;

        if (selectExpression is
            {
                GroupBy: [],
                Having: null,
                Projection: [],
                Orderings: [],
                Offset: null
            })
        {
            Sql.Append("DELETE ");
            GenerateTop(selectExpression);

            _withinTable = true;
            Sql.AppendLine($"FROM {Dependencies.SqlGenerationHelper.DelimitIdentifier(deleteExpression.Table.Alias)}");

            Sql.Append("FROM ");
            GenerateList(selectExpression.Tables, e => Visit(e), sql => sql.AppendLine());
            _withinTable = false;

            if (selectExpression.Predicate != null)
            {
                Sql.AppendLine().Append("WHERE ");

                Visit(selectExpression.Predicate);
            }

            GenerateLimitOffset(selectExpression);

            return deleteExpression;
        }

        throw new InvalidOperationException(
            RelationalStrings.ExecuteOperationWithUnsupportedOperatorInSqlGeneration(
                nameof(EntityFrameworkQueryableExtensions.ExecuteDelete)));
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitSelect(SelectExpression selectExpression)
    {
        // VistaDB, like SQL Server, requires column names in table subqueries; we track _withinTable to emit "1 AS empty".
        var parentWithinTable = _withinTable;
        base.VisitSelect(selectExpression);
        _withinTable = parentWithinTable;
        return selectExpression;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitUpdate(UpdateExpression updateExpression)
    {
        var selectExpression = updateExpression.SelectExpression;

        if (selectExpression is
            {
                GroupBy: [],
                Having: null,
                Projection: [],
                Orderings: [],
                Offset: null
            })
        {
            Sql.Append("UPDATE ");
            GenerateTop(selectExpression);

            Sql.AppendLine($"{Dependencies.SqlGenerationHelper.DelimitIdentifier(updateExpression.Table.Alias)}");
            Sql.Append("SET ");

            for (var i = 0; i < updateExpression.ColumnValueSetters.Count; i++)
            {
                var (column, value) = updateExpression.ColumnValueSetters[i];

                if (i == 1)
                {
                    Sql.IncrementIndent();
                }

                if (i > 0)
                {
                    Sql.AppendLine(",");
                }

                // VistaDB: no analog — SqlServer 2025 introduced a JSON modify() column setter; VistaDB has no JSON type or modify method.
                // Original SqlServer logic preserved below for future revival when VistaDB adds JSON support.
                /*
                    if (value is SqlFunctionExpression
                        {
                            Name: "modify",
                            IsBuiltIn: true,
                            Instance: ColumnExpression { TypeMapping.StoreType: "json" } instance
                        })
                    {
                        Visit(value);
                        continue;
                    }
                */

                Visit(column);
                Sql.Append(" = ");
                Visit(value);
            }

            if (updateExpression.ColumnValueSetters.Count > 1)
            {
                Sql.DecrementIndent();
            }

            _withinTable = true;
            Sql.AppendLine().Append("FROM ");
            GenerateList(selectExpression.Tables, e => Visit(e), sql => sql.AppendLine());
            _withinTable = false;

            if (selectExpression.Predicate != null)
            {
                Sql.AppendLine().Append("WHERE ");
                Visit(selectExpression.Predicate);
            }

            return updateExpression;
        }

        throw new InvalidOperationException(
            RelationalStrings.ExecuteOperationWithUnsupportedOperatorInSqlGeneration(
                nameof(EntityFrameworkQueryableExtensions.ExecuteUpdate)));
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitValues(ValuesExpression valuesExpression)
    {
        base.VisitValues(valuesExpression);

        // VistaDB supports SqlServer-style VALUES with column names: FROM (VALUES (1), (2)) AS v(foo).
        Sql.Append("(");

        for (var i = 0; i < valuesExpression.ColumnNames.Count; i++)
        {
            if (i > 0)
            {
                Sql.Append(", ");
            }

            Sql.Append(_sqlGenerationHelper.DelimitIdentifier(valuesExpression.ColumnNames[i]));
        }

        Sql.Append(")");

        return valuesExpression;
    }

    // VistaDB: no analog — SqlServer's VisitSqlConstant override here renders JSON path segments
    // (IReadOnlyList<PathSegment>) for JSON_MODIFY()-style functions; VistaDB has no JSON support.
    // Original SqlServer logic preserved below for future revival when VistaDB adds JSON support.
    /*
        protected override Expression VisitSqlConstant(SqlConstantExpression sqlConstantExpression)
        {
            if (sqlConstantExpression is { Value: IReadOnlyList<PathSegment> path })
            {
                GenerateJsonPath(path);
                return sqlConstantExpression;
            }

            return base.VisitSqlConstant(sqlConstantExpression);
        }
    */

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitSqlFunction(SqlFunctionExpression sqlFunctionExpression)
    {
        switch (sqlFunctionExpression)
        {
            case { IsBuiltIn: true, Arguments: not null }
                when string.Equals(sqlFunctionExpression.Name, "COALESCE", StringComparison.OrdinalIgnoreCase):
            {
                var type = sqlFunctionExpression.Type;
                var typeMapping = sqlFunctionExpression.TypeMapping;
                var defaultTypeMapping = _typeMappingSource.FindMapping(type);

                // Same logic as SqlServer: prefer ISNULL when type-mapping is homogeneous and default; otherwise leave as COALESCE.
                if (defaultTypeMapping == typeMapping
                    && sqlFunctionExpression.Arguments.All(a => a.Type == type && a.TypeMapping == typeMapping))
                {
                    var head = sqlFunctionExpression.Arguments[0];
                    sqlFunctionExpression = (SqlFunctionExpression)sqlFunctionExpression
                        .Arguments
                        .Skip(1)
                        .Aggregate(
                            head, (l, r) => new SqlFunctionExpression(
                                "ISNULL",
                                arguments: [l, r],
                                nullable: true,
                                argumentsPropagateNullability: [false, false],
                                sqlFunctionExpression.Type,
                                sqlFunctionExpression.TypeMapping
                            ));
                }

                return base.VisitSqlFunction(sqlFunctionExpression);
            }
        }

        // VistaDB: no analog — SqlServer additionally handles SqlServerJsonObjectExpression
        // (JSON_OBJECT() emission) and the SQL Server 2025 JSON modify() setter. VistaDB has no JSON support.
        // Original SqlServer logic preserved below for future revival when VistaDB adds JSON support.
        /*
            case SqlServerJsonObjectExpression jsonObject:
            {
                Sql.Append("JSON_OBJECT(");
                for (var i = 0; i < jsonObject.PropertyNames.Count; i++)
                {
                    if (i > 0)
                    {
                        Sql.Append(", ");
                    }

                    Sql.Append("'").Append(jsonObject.PropertyNames[i]).Append("': ");
                    Visit(jsonObject.Arguments![i]);
                }

                Sql.Append(")");
                return sqlFunctionExpression;
            }

            case
            {
                Name: "modify",
                IsBuiltIn: true,
                Instance: ColumnExpression { TypeMapping.StoreType: "json" } jsonColumn,
                Arguments: [SqlConstantExpression { Value: IReadOnlyList<PathSegment> jsonPath }, var item]
            }:
            {
                Sql
                    .Append(_sqlGenerationHelper.DelimitIdentifier(jsonColumn.Name))
                    .Append(".modify(");
                GenerateJsonPath(jsonPath);
                Sql.Append(", ");
                Visit(item);
                Sql.Append(")");
                return sqlFunctionExpression;
            }
        */

        return base.VisitSqlFunction(sqlFunctionExpression);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override void GenerateValues(ValuesExpression valuesExpression)
    {
        if (valuesExpression.RowValues is null)
        {
            throw new UnreachableException();
        }

        if (valuesExpression.RowValues.Count == 0)
        {
            throw new InvalidOperationException(RelationalStrings.EmptyCollectionNotSupportedAsInlineQueryRoot);
        }

        // VistaDB supports SqlServer-style VALUES (a, b), (c, d) AS x(p, q), so use the simple direct form.
        var rowValues = valuesExpression.RowValues;

        Sql.Append("VALUES ");

        for (var i = 0; i < rowValues.Count; i++)
        {
            if (i > 0)
            {
                Sql.Append(", ");
            }

            Visit(valuesExpression.RowValues[i]);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override void GenerateTop(SelectExpression selectExpression)
    {
        var parentWithinTable = _withinTable;
        _withinTable = false;

        if (selectExpression is { Limit: not null, Offset: null })
        {
            Sql.Append("TOP(");

            Visit(selectExpression.Limit);

            Sql.Append(") ");
        }

        _withinTable = parentWithinTable;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override void GenerateProjection(SelectExpression selectExpression)
    {
        if (selectExpression.Projection.Count == 0)
        {
            Sql.Append(_withinTable ? "1 AS empty" : "1");
        }
        else
        {
            var parentWithinTable = _withinTable;
            _withinTable = false;
            GenerateList(selectExpression.Projection, e => Visit(e));
            _withinTable = parentWithinTable;
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override void GenerateFrom(SelectExpression selectExpression)
    {
        _withinTable = true;
        base.GenerateFrom(selectExpression);
        _withinTable = false;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override void GenerateOrderings(SelectExpression selectExpression)
    {
        if (selectExpression.Orderings.Any())
        {
            var orderings = selectExpression.Orderings.ToList();

            if (selectExpression.Limit == null && selectExpression.Offset == null)
            {
                orderings.RemoveAll(oe => oe.Expression is SqlConstantExpression or SqlParameterExpression);
            }

            if (orderings.Count > 0)
            {
                Sql.AppendLine().Append("ORDER BY ");

                var first = true;
                foreach (var ordering in orderings)
                {
                    if (!first)
                    {
                        Sql.Append(", ");
                    }

                    first = false;

                    // VistaDB rejects complex expressions (CAST, UNICODE, function calls) in ORDER BY
                    // (parse error 632). When the ordering expression matches a SELECT projection, emit
                    // its 1-based ordinal position instead so VistaDB resolves it via the projected alias.
                    var matchedProjectionIndex = -1;
                    if (selectExpression.Projection.Count > 0)
                    {
                        for (var i = 0; i < selectExpression.Projection.Count; i++)
                        {
                            if (ExpressionEqualityComparer.Instance.Equals(
                                    selectExpression.Projection[i].Expression, ordering.Expression))
                            {
                                matchedProjectionIndex = i;
                                break;
                            }
                        }
                    }

                    if (matchedProjectionIndex >= 0)
                    {
                        Sql.Append((matchedProjectionIndex + 1).ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        Visit(ordering.Expression);
                    }

                    if (!ordering.IsAscending)
                    {
                        Sql.Append(" DESC");
                    }
                }
            }
        }

        // VistaDB, like SQL Server, requires ORDER BY when OFFSET is used.
        if (!selectExpression.Orderings.Any() && selectExpression.Offset != null)
        {
            Sql.AppendLine().Append("ORDER BY (SELECT 1)");
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override void GenerateLimitOffset(SelectExpression selectExpression)
    {
        // Limit-only is handled by TOP(); Limit+Offset (and Offset-only) uses OFFSET ... FETCH NEXT, which VistaDB supports.
        if (selectExpression.Offset != null)
        {
            Sql.AppendLine()
                .Append("OFFSET ");

            Visit(selectExpression.Offset);

            Sql.Append(" ROWS");

            if (selectExpression.Limit != null)
            {
                Sql.Append(" FETCH NEXT ");

                Visit(selectExpression.Limit);

                Sql.Append(" ROWS ONLY");
            }
        }
    }

    // VistaDB: no analog — SqlServer overrides VisitExtension to handle TemporalQueryRoot tables
    // ("FOR SYSTEM_TIME ..."), SqlServerAggregateFunctionExpression ("WITHIN GROUP (ORDER BY ...)"),
    // and SqlServerOpenJsonExpression. VistaDB supports neither temporal tables, custom aggregates, nor JSON.
    // Original SqlServer logic preserved below for future revival when VistaDB adds support.
    /*
        protected override Expression VisitExtension(Expression extensionExpression)
        {
            switch (extensionExpression)
            {
                case TableExpression tableExpression
                    when tableExpression.FindAnnotation(SqlServerAnnotationNames.TemporalOperationType) != null:
                {
                    // ... emits FOR SYSTEM_TIME AS OF / BETWEEN / FROM ... TO / CONTAINED IN / ALL
                }

                case SqlServerAggregateFunctionExpression aggregateFunctionExpression:
                    return VisitSqlServerAggregateFunction(aggregateFunctionExpression);

                case SqlServerOpenJsonExpression openJsonExpression:
                    return VisitOpenJsonExpression(openJsonExpression);
            }

            return base.VisitExtension(extensionExpression);
        }

        protected virtual Expression VisitSqlServerAggregateFunction(SqlServerAggregateFunctionExpression aggregateFunctionExpression)
        {
            Sql.Append(aggregateFunctionExpression.Name);
            Sql.Append("(");
            GenerateList(aggregateFunctionExpression.Arguments, e => Visit(e));
            Sql.Append(")");

            if (aggregateFunctionExpression.Orderings.Count > 0)
            {
                Sql.Append(" WITHIN GROUP (ORDER BY ");
                GenerateList(aggregateFunctionExpression.Orderings, e => Visit(e));
                Sql.Append(")");
            }

            return aggregateFunctionExpression;
        }
    */

    // VistaDB: no analog — JsonScalarExpression renders SqlServer's JSON_VALUE/JSON_QUERY with
    // the SQL Server 2025 RETURNING clause; VistaDB has no JSON functions.
    // Original SqlServer logic preserved below for future revival when VistaDB adds JSON support.
    /*
        protected override Expression VisitJsonScalar(JsonScalarExpression jsonScalarExpression)
        {
            // ... emits JSON_VALUE(json, path RETURNING type) or JSON_QUERY(json, path), with CAST wrappers.
        }

        private void GenerateJsonPath(IReadOnlyList<PathSegment> path)
        {
            // ... emits '$.foo[idx]' etc, including the SqlServer-2017+ dynamic-index case.
        }

        protected virtual Expression VisitOpenJsonExpression(SqlServerOpenJsonExpression openJsonExpression)
        {
            // ... emits OPENJSON(json, path) WITH (col1 type1 path1, col2 type2 path2, ...) AS alias.
        }
    */

    /// <inheritdoc />
    protected override void CheckComposableSqlTrimmed(ReadOnlySpan<char> sql)
    {
        base.CheckComposableSqlTrimmed(sql);

        // VistaDB does not support CTEs (WITH ... AS); a FromSql starting with WITH is non-composable.
        if (sql.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(VistaDBStrings.CteNotSupported);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override bool TryGetOperatorInfo(SqlExpression expression, out int precedence, out bool isAssociative)
    {
        // See https://docs.microsoft.com/sql/t-sql/language-elements/operator-precedence-transact-sql; VistaDB follows T-SQL precedence.
        (precedence, isAssociative) = expression switch
        {
            SqlBinaryExpression sqlBinaryExpression => sqlBinaryExpression.OperatorType switch
            {
                ExpressionType.Multiply => (900, true),
                ExpressionType.Divide => (900, false),
                ExpressionType.Modulo => (900, false),
                ExpressionType.Add => (700, true),
                ExpressionType.Subtract => (700, false),
                ExpressionType.And => (700, true),
                ExpressionType.Or => (700, true),
                ExpressionType.ExclusiveOr => (700, true),
                ExpressionType.LeftShift => (700, true),
                ExpressionType.RightShift => (700, true),
                ExpressionType.LessThan => (500, false),
                ExpressionType.LessThanOrEqual => (500, false),
                ExpressionType.GreaterThan => (500, false),
                ExpressionType.GreaterThanOrEqual => (500, false),
                ExpressionType.Equal => (500, false),
                ExpressionType.NotEqual => (500, false),
                ExpressionType.AndAlso => (200, true),
                ExpressionType.OrElse => (100, true),

                _ => default,
            },

            SqlUnaryExpression sqlUnaryExpression => sqlUnaryExpression.OperatorType switch
            {
                ExpressionType.Convert => (1300, false),
                ExpressionType.OnesComplement => (1200, false),
                ExpressionType.Not when sqlUnaryExpression.Type != typeof(bool) => (1200, false),
                ExpressionType.Negate => (1100, false),
                ExpressionType.Equal => (500, false), // IS NULL
                ExpressionType.NotEqual => (500, false), // IS NOT NULL
                ExpressionType.Not when sqlUnaryExpression.Type == typeof(bool) => (300, false),

                _ => default,
            },

            CollateExpression => (900, false),
            LikeExpression => (350, false),

            // VistaDB: no analog — AtTimeZoneExpression maps to "AT TIME ZONE", which VistaDB does not support.
            // The translator stack never produces this node for VistaDB; leaving precedence undefined causes a
            // parenthesisation fallback should one ever appear.
            /*
                AtTimeZoneExpression => (1200, false),
                JsonScalarExpression => (9999, false),
            */

            _ => default,
        };

        return precedence != default;
    }

    private void GenerateList<T>(
        IReadOnlyList<T> items,
        Action<T> generationAction,
        Action<IRelationalCommandBuilder>? joinAction = null)
    {
        joinAction ??= (isb => isb.Append(", "));

        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                joinAction(Sql);
            }

            generationAction(items[i]);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    ///     <para>
    ///         VistaDB does not support window functions. <c>ROW_NUMBER() OVER (PARTITION BY ... ORDER BY ...)</c>
    ///         — used by EF Core's split-collection / paginated-collection query translation — is rejected at
    ///         the parser level (engine errors 509 / 507 — verified by <c>WindowFunctionProbeTest</c>).
    ///         Surface a clear <see cref="NotSupportedException" /> at query-generation time so the failure
    ///         is attributable to the missing feature rather than a downstream parser error.
    ///     </para>
    ///     <para>
    ///         Original base behavior preserved for future revival when VistaDB adds support:
    ///         <c>=> base.VisitRowNumber(rowNumberExpression);</c>.
    ///     </para>
    /// </remarks>
    protected override Expression VisitRowNumber(RowNumberExpression rowNumberExpression)
        => throw new NotSupportedException(VistaDBStrings.WindowFunctionsNotSupported);

}
