// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Data;
using Microsoft.EntityFrameworkCore.VistaDB.Infrastructure.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBTypeMappingSource(
    TypeMappingSourceDependencies dependencies,
    RelationalTypeMappingSourceDependencies relationalDependencies,
    IVistaDBSingletonOptions vistaDBSingletonOptions)
    : RelationalTypeMappingSource(dependencies, relationalDependencies)
{
    // VistaDB: vistaDBSingletonOptions reserved for future flags (json, compatibility levels). Currently unused.
    private readonly IVistaDBSingletonOptions _vistaDBSingletonOptions = vistaDBSingletonOptions;

    private static readonly VistaDBFloatTypeMapping RealAlias
        = new("placeholder", storeTypePostfix: StoreTypePostfix.None);

    private static readonly VistaDBByteArrayTypeMapping Rowversion
        = new(
            "rowversion",
            size: 8,
            comparer: new ValueComparer<byte[]>(
                (v1, v2) => StructuralComparisons.StructuralEqualityComparer.Equals(v1, v2),
                v => StructuralComparisons.StructuralEqualityComparer.GetHashCode(v),
                v => v.ToArray()),
            storeTypePostfix: StoreTypePostfix.None);

    private static readonly VistaDBLongTypeMapping LongRowversion
        = new(
            "rowversion",
            converter: new NumberToBytesConverter<long>(),
            providerValueComparer: new ValueComparer<byte[]>(
                (v1, v2) => StructuralComparisons.StructuralEqualityComparer.Equals(v1, v2),
                v => StructuralComparisons.StructuralEqualityComparer.GetHashCode(v),
                v => v.ToArray()),
            dbType: DbType.Binary);

    private static readonly VistaDBLongTypeMapping UlongRowversion
        = new(
            "rowversion",
            converter: new NumberToBytesConverter<ulong>(),
            providerValueComparer: new ValueComparer<byte[]>(
                (v1, v2) => StructuralComparisons.StructuralEqualityComparer.Equals(v1, v2),
                v => StructuralComparisons.StructuralEqualityComparer.GetHashCode(v),
                v => v.ToArray()),
            dbType: DbType.Binary);

    private static readonly VistaDBStringTypeMapping FixedLengthUnicodeString
        = new(unicode: true, fixedLength: true);

    private static readonly VistaDBStringTypeMapping TextUnicodeString
        = new("ntext", unicode: true, storeTypePostfix: StoreTypePostfix.None);

    private static readonly VistaDBStringTypeMapping VariableLengthUnicodeString
        = new(unicode: true);

    private static readonly VistaDBStringTypeMapping VariableLengthMaxUnicodeString
        = new("nvarchar(max)", unicode: true, storeTypePostfix: StoreTypePostfix.None);

    private static readonly VistaDBStringTypeMapping FixedLengthAnsiString
        = new(fixedLength: true);

    private static readonly VistaDBStringTypeMapping TextAnsiString
        = new("text", storeTypePostfix: StoreTypePostfix.None);

    private static readonly VistaDBStringTypeMapping VariableLengthMaxAnsiString
        = new("varchar(max)", storeTypePostfix: StoreTypePostfix.None);

    private static readonly VistaDBByteArrayTypeMapping ImageBinary
        = new("image");

    private static readonly VistaDBByteArrayTypeMapping VariableLengthMaxBinary
        = new("varbinary(max)", storeTypePostfix: StoreTypePostfix.None);

    private static readonly VistaDBByteArrayTypeMapping FixedLengthBinary
        = new(fixedLength: true);

    private static readonly VistaDBDateTimeTypeMapping DateAsDateTime
        = new("date", DbType.Date);

    private static readonly VistaDBDateTimeTypeMapping SmallDatetime
        = new("smalldatetime", DbType.DateTime);

    private static readonly VistaDBDateTimeTypeMapping Datetime
        = new("datetime", DbType.DateTime);

    private static readonly VistaDBDateTimeTypeMapping Datetime2Alias
        = new("placeholder", DbType.DateTime2, StoreTypePostfix.None);

    private static readonly DoubleTypeMapping DoubleAlias
        = new VistaDBDoubleTypeMapping("placeholder", storeTypePostfix: StoreTypePostfix.None);

    private static readonly VistaDBDateTimeOffsetTypeMapping DatetimeoffsetAlias
        = new("placeholder", DbType.DateTimeOffset, StoreTypePostfix.None);

    private static readonly VistaDBDecimalTypeMapping Decimal
        = new("decimal", precision: 18, scale: 0);

    private static readonly VistaDBDecimalTypeMapping DecimalAlias
        = new("placeholder", precision: 18, scale: 2, storeTypePostfix: StoreTypePostfix.None);

    private static readonly VistaDBDecimalTypeMapping Money
        = new("money", DbType.Currency, storeTypePostfix: StoreTypePostfix.None);

    private static readonly VistaDBDecimalTypeMapping SmallMoney
        = new("smallmoney", DbType.Currency, storeTypePostfix: StoreTypePostfix.None);

    private static readonly VistaDBTimeOnlyTypeMapping TimeAlias
        = new("placeholder", StoreTypePostfix.None);

    private static readonly GuidTypeMapping Uniqueidentifier
        = new("uniqueidentifier");

    private static readonly VistaDBStringTypeMapping Xml
        = new("xml", unicode: true, storeTypePostfix: StoreTypePostfix.None);

    private static readonly Dictionary<Type, RelationalTypeMapping> _clrTypeMappings;

    private static readonly Dictionary<Type, RelationalTypeMapping> _clrNoFacetTypeMappings;

    private static readonly Dictionary<string, RelationalTypeMapping[]> _storeTypeMappings;

    static VistaDBTypeMappingSource()
    {
        _clrTypeMappings
            = new Dictionary<Type, RelationalTypeMapping>
            {
                { typeof(int), IntTypeMapping.Default },
                { typeof(long), VistaDBLongTypeMapping.Default },
                { typeof(DateOnly), VistaDBDateOnlyTypeMapping.Default },
                { typeof(DateTime), VistaDBDateTimeTypeMapping.Default },
                { typeof(Guid), Uniqueidentifier },
                { typeof(bool), VistaDBBoolTypeMapping.Default },
                { typeof(byte), VistaDBByteTypeMapping.Default },
                { typeof(double), VistaDBDoubleTypeMapping.Default },
                { typeof(DateTimeOffset), VistaDBDateTimeOffsetTypeMapping.Default },
                { typeof(short), VistaDBShortTypeMapping.Default },
                { typeof(float), VistaDBFloatTypeMapping.Default },
                { typeof(decimal), VistaDBDecimalTypeMapping.Default },
                { typeof(TimeOnly), VistaDBTimeOnlyTypeMapping.Default },
                { typeof(TimeSpan), VistaDBTimeSpanTypeMapping.Default }
            };

        _clrNoFacetTypeMappings
            = new Dictionary<Type, RelationalTypeMapping>
            {
                { typeof(DateTime), Datetime2Alias },
                { typeof(DateTimeOffset), DatetimeoffsetAlias },
                { typeof(TimeOnly), TimeAlias },
                { typeof(double), DoubleAlias },
                { typeof(float), RealAlias },
                { typeof(decimal), DecimalAlias }
            };

        // ReSharper disable CoVariantArrayConversion
        _storeTypeMappings
            = new Dictionary<string, RelationalTypeMapping[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "bigint", [VistaDBLongTypeMapping.Default] },
                { "binary varying", [VistaDBByteArrayTypeMapping.Default] },
                { "binary", [FixedLengthBinary] },
                { "bit", [VistaDBBoolTypeMapping.Default] },
                { "char varying", [VistaDBStringTypeMapping.Default] },
                { "char varying(max)", [VariableLengthMaxAnsiString] },
                { "char", [FixedLengthAnsiString] },
                { "character varying", [VistaDBStringTypeMapping.Default] },
                { "character varying(max)", [VariableLengthMaxAnsiString] },
                { "character", [FixedLengthAnsiString] },
                { "date", [VistaDBDateOnlyTypeMapping.Default, DateAsDateTime] },
                { "datetime", [Datetime] },
                { "datetime2", [VistaDBDateTimeTypeMapping.Default] },
                { "datetimeoffset", [VistaDBDateTimeOffsetTypeMapping.Default] },
                { "dec", [Decimal] },
                { "decimal", [Decimal] },
                { "double precision", [VistaDBDoubleTypeMapping.Default] },
                { "float", [VistaDBDoubleTypeMapping.Default] },
                { "image", [ImageBinary] },
                { "int", [IntTypeMapping.Default] },
                { "money", [Money] },
                { "national char varying", [VariableLengthUnicodeString] },
                { "national char varying(max)", [VariableLengthMaxUnicodeString] },
                { "national character varying", [VariableLengthUnicodeString] },
                { "national character varying(max)", [VariableLengthMaxUnicodeString] },
                { "national character", [FixedLengthUnicodeString] },
                { "nchar", [FixedLengthUnicodeString] },
                { "ntext", [TextUnicodeString] },
                { "numeric", [Decimal] },
                { "nvarchar", [VariableLengthUnicodeString] },
                { "nvarchar(max)", [VariableLengthMaxUnicodeString] },
                { "real", [VistaDBFloatTypeMapping.Default] },
                { "rowversion", [Rowversion] },
                { "smalldatetime", [SmallDatetime] },
                { "smallint", [VistaDBShortTypeMapping.Default] },
                { "smallmoney", [SmallMoney] },
                { "text", [TextAnsiString] },
                { "time", [VistaDBTimeOnlyTypeMapping.Default, VistaDBTimeSpanTypeMapping.Default] },
                { "timestamp", [Rowversion] },
                { "tinyint", [VistaDBByteTypeMapping.Default] },
                { "uniqueidentifier", [Uniqueidentifier] },
                { "varbinary", [VistaDBByteArrayTypeMapping.Default] },
                { "varbinary(max)", [VariableLengthMaxBinary] },
                { "varchar", [VistaDBStringTypeMapping.Default] },
                { "varchar(max)", [VariableLengthMaxAnsiString] },
                { "xml", [Xml] }
            };
        // ReSharper restore CoVariantArrayConversion

        // VistaDB: no analog — VistaDB does not support sql_variant, the SQL Server structural JSON type,
        // or the vector data type. Original SqlServer store-type entries preserved below for future revival.
        /*
            { "json", [SqlServerStringTypeMapping.JsonTypeDefault] },
            { "sql_variant", [SqlServerSqlVariantTypeMapping.Default] },
            { "vector", [SqlServerVectorTypeMapping.Default] },
        */
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override RelationalTypeMapping? FindMapping(in RelationalTypeMappingInfo mappingInfo)
        => base.FindMapping(mappingInfo)
            ?? FindRawMapping(mappingInfo)?.WithTypeMappingInfo(mappingInfo);

    private RelationalTypeMapping? FindRawMapping(RelationalTypeMappingInfo mappingInfo)
    {
        var clrType = mappingInfo.ClrType;
        var storeTypeName = mappingInfo.StoreTypeName;

        // VistaDB: no analog — VistaDB does not support the SQL Server structural JSON type.
        // Original SqlServer logic preserved below for future revival when VistaDB adds JSON support.
        /*
            if (clrType == typeof(JsonTypePlaceholder))
            {
                return storeTypeName switch
                {
                    "json" => SqlServerStructuralJsonTypeMapping.JsonTypeDefault,
                    "nvarchar(max)" => SqlServerStructuralJsonTypeMapping.NvarcharMaxDefault,
                    null when _isJsonTypeSupported => SqlServerStructuralJsonTypeMapping.JsonTypeDefault,
                    null => SqlServerStructuralJsonTypeMapping.NvarcharMaxDefault,
                    _ => null
                };
            }
        */

        if (storeTypeName != null)
        {
            var storeTypeNameBase = mappingInfo.StoreTypeNameBase;
            if (storeTypeNameBase!.StartsWith("[", StringComparison.Ordinal)
                && storeTypeNameBase.EndsWith("]", StringComparison.Ordinal))
            {
                storeTypeNameBase = storeTypeNameBase[1..^1];
            }

            if (clrType == typeof(float)
                && mappingInfo.Precision is <= 24
                && (storeTypeNameBase.Equals("float", StringComparison.OrdinalIgnoreCase)
                    || storeTypeNameBase.Equals("double precision", StringComparison.OrdinalIgnoreCase)))
            {
                return VistaDBFloatTypeMapping.Default;
            }

            if (_storeTypeMappings.TryGetValue(storeTypeName, out var mappings)
                || _storeTypeMappings.TryGetValue(storeTypeNameBase, out mappings))
            {
                if (clrType is null)
                {
                    return mappings[0];
                }

                foreach (var m in mappings)
                {
                    if (m.ClrType == clrType)
                    {
                        return m;
                    }
                }

                return null;
            }

            if (clrType != null
                && _clrNoFacetTypeMappings.TryGetValue(clrType, out var mapping))
            {
                return mapping;
            }
        }

        if (clrType != null)
        {
            if (_clrTypeMappings.TryGetValue(clrType, out var mapping))
            {
                return mapping;
            }

            switch (clrType)
            {
                case { } t when t == typeof(ulong) && mappingInfo.IsRowVersion is true:
                    return UlongRowversion;

                case { } t when t == typeof(long) && mappingInfo.IsRowVersion is true:
                    return LongRowversion;

                case { } t when t == typeof(byte[]) && mappingInfo.IsRowVersion is true:
                    return Rowversion;

                case { } t when t == typeof(string):
                {
                    var isAnsi = mappingInfo.IsUnicode == false;
                    var isFixedLength = mappingInfo.IsFixedLength == true;
                    var maxSize = isAnsi ? 8000 : 4000;

                    var size = mappingInfo.Size ?? (mappingInfo.IsKeyOrIndex ? isAnsi ? 900 : 450 : null);
                    if (size < 0 || size > maxSize)
                    {
                        size = isFixedLength ? maxSize : null;
                    }

                    if (size == null
                        && storeTypeName == null
                        && !mappingInfo.IsKeyOrIndex)
                    {
                        return isAnsi
                            ? isFixedLength
                                ? FixedLengthAnsiString
                                : VariableLengthMaxAnsiString
                            : isFixedLength
                                ? FixedLengthUnicodeString
                                : VariableLengthMaxUnicodeString;
                    }

                    return new VistaDBStringTypeMapping(
                        unicode: !isAnsi,
                        size: size,
                        fixedLength: isFixedLength,
                        storeTypePostfix: storeTypeName == null ? StoreTypePostfix.Size : StoreTypePostfix.None,
                        useKeyComparison: mappingInfo.IsKey);
                }

                case { } t when t == typeof(byte[]) && mappingInfo.ElementTypeMapping is null:
                {
                    var isFixedLength = mappingInfo.IsFixedLength == true;

                    var size = mappingInfo.Size ?? (mappingInfo.IsKeyOrIndex ? 900 : null);
                    if (size is < 0 or > 8000)
                    {
                        size = isFixedLength ? 8000 : null;
                    }

                    return size == null
                        ? VariableLengthMaxBinary
                        : new VistaDBByteArrayTypeMapping(
                            size: size,
                            fixedLength: isFixedLength,
                            storeTypePostfix: storeTypeName == null ? StoreTypePostfix.Size : StoreTypePostfix.None);
                }

                // VistaDB: no analog — VistaDB does not support the SqlVector<float> type.
                // Original SqlServer branch preserved below for future revival.
                /*
                    case { } t when t == typeof(SqlVector<float>):
                        return new SqlServerVectorTypeMapping(mappingInfo.Size);
                */
            }
        }

        return null;
    }

    private static readonly List<string> NameBasesUsingPrecision =
    [
        "decimal",
        "dec",
        "numeric",
        "datetime2",
        "datetimeoffset",
        "double precision",
        "float",
        "time"
    ];

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override string? ParseStoreTypeName(
        string? storeTypeName,
        ref bool? unicode,
        ref int? size,
        ref int? precision,
        ref int? scale)
    {
        if (storeTypeName == null)
        {
            return null;
        }

        var originalSize = size;
        var parsedName = base.ParseStoreTypeName(storeTypeName, ref unicode, ref size, ref precision, ref scale);

        if (size.HasValue
            && NameBasesUsingPrecision.Any(n => storeTypeName.StartsWith(n, StringComparison.OrdinalIgnoreCase)))
        {
            precision = size;
            size = originalSize;
        }
        else if (storeTypeName.Trim().EndsWith("(max)", StringComparison.OrdinalIgnoreCase))
        {
            size = -1;
        }

        return parsedName;
    }
}
