// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using Microsoft.EntityFrameworkCore.Storage.Json;

namespace Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBDateTimeTypeMapping : DateTimeTypeMapping
{
    private const string DateFormatConst = "'{0:yyyy-MM-dd}'";
    private const string SmallDateTimeFormatConst = "'{0:yyyy-MM-ddTHH:mm:ss}'";
    private const string DateTimeFormatConst = "'{0:yyyy-MM-ddTHH:mm:ss.fff}'";

    // Note: this array will be accessed using the precision as an index
    // so the order of the entries in this array is important
    private readonly string[] _dateTime2Formats =
    [
        "'{0:yyyy-MM-ddTHH:mm:ssK}'",
        "'{0:yyyy-MM-ddTHH:mm:ss.fK}'",
        "'{0:yyyy-MM-ddTHH:mm:ss.ffK}'",
        "'{0:yyyy-MM-ddTHH:mm:ss.fffK}'",
        "'{0:yyyy-MM-ddTHH:mm:ss.ffffK}'",
        "'{0:yyyy-MM-ddTHH:mm:ss.fffffK}'",
        "'{0:yyyy-MM-ddTHH:mm:ss.ffffffK}'",
        "'{0:yyyy-MM-ddTHH:mm:ss.fffffffK}'"
    ];

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static new VistaDBDateTimeTypeMapping Default { get; } = new("datetime2");

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBDateTimeTypeMapping(
        string storeType,
        DbType? dbType = System.Data.DbType.DateTime2,
        StoreTypePostfix storeTypePostfix = StoreTypePostfix.Precision)
        : base(
            new RelationalTypeMappingParameters(
                new CoreTypeMappingParameters(typeof(DateTime), jsonValueReaderWriter: JsonDateTimeReaderWriter.Instance),
                storeType,
                storeTypePostfix,
                dbType))
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected VistaDBDateTimeTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override void ConfigureParameter(DbParameter parameter)
    {
        base.ConfigureParameter(parameter);

        if (Size.HasValue
            && Size.Value != -1)
        {
            parameter.Size = Size.Value;
        }

        if (Precision.HasValue)
        {
            parameter.Scale = (byte)Precision.Value;
        }
    }

    /// <summary>
    ///     Creates a copy of this mapping.
    /// </summary>
    /// <param name="parameters">The parameters for this mapping.</param>
    /// <returns>The newly created mapping.</returns>
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new VistaDBDateTimeTypeMapping(parameters);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override string SqlLiteralFormatString
        => StoreType switch
        {
            "date" => DateFormatConst,
            "datetime" => DateTimeFormatConst,
            "smalldatetime" => SmallDateTimeFormatConst,
            _ => _dateTime2Formats[Precision is >= 0 and <= 7 ? Precision.Value : 7]
        };
}
