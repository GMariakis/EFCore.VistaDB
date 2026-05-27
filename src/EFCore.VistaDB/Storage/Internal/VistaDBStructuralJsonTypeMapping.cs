// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBStructuralJsonTypeMapping : JsonTypeMapping
{
    // VistaDB: no analog — VistaDB does not support the SQL Server structural JSON type.
    // Original SqlServer logic preserved below for future revival when VistaDB adds support.
    /*
        public static SqlServerStructuralJsonTypeMapping Default => JsonTypeDefault;
        public static SqlServerStructuralJsonTypeMapping JsonTypeDefault { get; } = new("json");
        public static SqlServerStructuralJsonTypeMapping NvarcharMaxDefault { get; } = new("nvarchar(max)");

        public SqlServerStructuralJsonTypeMapping(string storeType)
            : base(storeType, typeof(JsonTypePlaceholder), System.Data.DbType.String) { }

        public override MethodInfo GetDataReaderMethod() => GetStringMethod;

        public static MemoryStream CreateUtf8Stream(string json)
            => json == ""
                ? throw new InvalidOperationException(RelationalStrings.JsonEmptyString)
                : new MemoryStream(Encoding.UTF8.GetBytes(json));

        public override Expression CustomizeDataReaderExpression(Expression expression)
            => Expression.Call(CreateUtf8StreamMethod, expression);

        protected SqlServerStructuralJsonTypeMapping(RelationalTypeMappingParameters parameters) : base(parameters) { }
        protected virtual string EscapeSqlLiteral(string literal) => literal.Replace("'", "''");
        protected override string GenerateNonNullSqlLiteral(object value) => $"'{EscapeSqlLiteral((string)value)}'";

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => new SqlServerStructuralJsonTypeMapping(parameters);

        protected override void ConfigureParameter(DbParameter parameter)
        {
            if (StoreType == "json" && parameter is SqlParameter sqlParameter)
            {
                sqlParameter.SqlDbType = SqlDbType.Json;
            }
            base.ConfigureParameter(parameter);
        }
    */
}
