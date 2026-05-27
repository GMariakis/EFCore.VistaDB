// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBUdtTypeMapping : RelationalTypeMapping
{
    // VistaDB: no analog — VistaDB does not support user-defined CLR types.
    // Original SqlServer logic preserved below for future revival when VistaDB adds support.
    /*
        private static Action<DbParameter, string>? _udtTypeNameSetter;

        public SqlServerUdtTypeMapping(
            Type clrType, string storeType, Func<object, Expression> literalGenerator,
            StoreTypePostfix storeTypePostfix = StoreTypePostfix.None, string? udtTypeName = null,
            ValueConverter? converter = null, ValueComparer? comparer = null, ValueComparer? keyComparer = null,
            DbType? dbType = null, bool unicode = false, int? size = null, bool fixedLength = false,
            int? precision = null, int? scale = null)
            : base(...) { ... }

        public virtual string UdtTypeName { get; }
        public virtual Func<object, Expression> LiteralGenerator { get; }

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => new SqlServerUdtTypeMapping(parameters, LiteralGenerator, UdtTypeName);

        protected override void ConfigureParameter(DbParameter parameter) => SetUdtTypeName(parameter);

        public override Expression GenerateCodeLiteral(object value) => LiteralGenerator(value);
    */
}
