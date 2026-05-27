// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
/// <remarks>
///     VistaDB.6 6.6.2 predates .NET 6's <see cref="DateOnly" /> type and its ADO.NET parameter binding
///     can't unbox a <c>DateOnly</c> value — it casts internally to <see cref="DateTime" /> and throws
///     <see cref="InvalidCastException" /> "Unable to cast object of type 'System.DateOnly' to type
///     'System.DateTime'". The reader path has the symmetric problem reading values back. We attach a
///     <see cref="ValueConverter{TModel,TProvider}" /> so EF Core converts at the boundary in both
///     directions: <c>DateOnly</c> ↔ <see cref="DateTime" /> at midnight. The provider sees a type it
///     understands; the model keeps its <c>DateOnly</c> shape.
/// </remarks>
public class VistaDBDateOnlyTypeMapping : DateOnlyTypeMapping
{
    private static readonly ValueConverter<DateOnly, DateTime> DateOnlyDateTimeConverter
        = new(d => d.ToDateTime(TimeOnly.MinValue), dt => DateOnly.FromDateTime(dt));

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static new VistaDBDateOnlyTypeMapping Default { get; } = new("date");

    internal VistaDBDateOnlyTypeMapping(string storeType)
        : base(
            new RelationalTypeMappingParameters(
                new CoreTypeMappingParameters(typeof(DateOnly), DateOnlyDateTimeConverter),
                storeType,
                StoreTypePostfix.None,
                System.Data.DbType.Date))
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected VistaDBDateOnlyTypeMapping(RelationalTypeMappingParameters parameters)
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
        => new VistaDBDateOnlyTypeMapping(parameters);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override string SqlLiteralFormatString
        => "'{0:yyyy-MM-dd}'";
}
