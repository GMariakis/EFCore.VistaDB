// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBVectorTypeMapping : RelationalTypeMapping
{
    // VistaDB: no analog — VistaDB does not support the vector data type.
    // Original SqlServer logic preserved below for future revival when VistaDB adds support.
    /*
        public static SqlServerVectorTypeMapping Default { get; } = new(dimensions: null);
        private static readonly VectorComparer _comparerInstance = new();

        public SqlServerVectorTypeMapping(int? dimensions)
            : this(new RelationalTypeMappingParameters(
                new CoreTypeMappingParameters(typeof(SqlVector<float>), comparer: _comparerInstance),
                "vector", StoreTypePostfix.Size, size: dimensions))
        {
            if (dimensions is <= 0)
                throw new InvalidOperationException(SqlServerStrings.VectorDimensionsInvalid);
        }

        protected SqlServerVectorTypeMapping(RelationalTypeMappingParameters parameters) : base(parameters) { ... }

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => new SqlServerVectorTypeMapping(parameters);

        protected override string GenerateNonNullSqlLiteral(object value)
        {
            // Constructs CAST('[v0,v1,...]' AS VECTOR(<dim>)) literal for SQL Server.
            ...
        }

        private sealed class VectorComparer : ValueComparer<SqlVector<float>> { ... }
    */
}
