// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBSqlVariantTypeMapping : RelationalTypeMapping
{
    // VistaDB: no analog — VistaDB does not support the sql_variant data type.
    // Original SqlServer logic preserved below for future revival when VistaDB adds support.
    /*
        public static SqlServerSqlVariantTypeMapping Default { get; } = new("sql_variant");

        public SqlServerSqlVariantTypeMapping(string storeType)
            : base(storeType, typeof(object), System.Data.DbType.Object)
        {
        }

        protected SqlServerSqlVariantTypeMapping(RelationalTypeMappingParameters parameters)
            : base(parameters)
        {
        }

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => new SqlServerSqlVariantTypeMapping(parameters);
    */
}
