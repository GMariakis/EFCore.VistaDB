// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.Json;

namespace Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBTimeOnlyTypeMapping : TimeOnlyTypeMapping
{
    // Note: this array will be accessed using the precision as an index
    // so the order of the entries in this array is important
    private readonly string[] _timeFormats =
    [
        @"'{0:HH\:mm\:ss}'",
        @"'{0:HH\:mm\:ss\.F}'",
        @"'{0:HH\:mm\:ss\.FF}'",
        @"'{0:HH\:mm\:ss\.FFF}'",
        @"'{0:HH\:mm\:ss\.FFFF}'",
        @"'{0:HH\:mm\:ss\.FFFFF}'",
        @"'{0:HH\:mm\:ss\.FFFFFF}'",
        @"'{0:HH\:mm\:ss\.FFFFFFF}'"
    ];

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static new VistaDBTimeOnlyTypeMapping Default { get; } = new("time");

    internal VistaDBTimeOnlyTypeMapping(string storeType, StoreTypePostfix storeTypePostfix = StoreTypePostfix.Precision)
        : base(
            new RelationalTypeMappingParameters(
                new CoreTypeMappingParameters(typeof(TimeOnly), jsonValueReaderWriter: JsonTimeOnlyReaderWriter.Instance),
                storeType,
                storeTypePostfix,
                System.Data.DbType.Time))
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected VistaDBTimeOnlyTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new VistaDBTimeOnlyTypeMapping(parameters);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override void ConfigureParameter(DbParameter parameter)
    {
        base.ConfigureParameter(parameter);

        if (Precision.HasValue)
        {
            parameter.Scale = unchecked((byte)Precision.Value);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override string SqlLiteralFormatString
        => _timeFormats[Precision is >= 0 and <= 7 ? Precision.Value : 7];

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override string GenerateNonNullSqlLiteral(object value)
        => ((TimeOnly)value).Ticks % 10000000 == 0 // Handle trailing decimal separator when no fractional seconds
            ? string.Format(CultureInfo.InvariantCulture, _timeFormats[0], value)
            : string.Format(CultureInfo.InvariantCulture, SqlLiteralFormatString, value);
}
