// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBMethodCallTranslatorProvider : RelationalMethodCallTranslatorProvider
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBMethodCallTranslatorProvider(RelationalMethodCallTranslatorProviderDependencies dependencies)
        : base(dependencies)
    {
        var sqlExpressionFactory = dependencies.SqlExpressionFactory;

        var typeMappingSource = dependencies.RelationalTypeMappingSource;

        AddTranslators(
        [
            new VistaDBMathTranslator(sqlExpressionFactory),
            new VistaDBNewGuidTranslator(sqlExpressionFactory),
            new VistaDBStringMethodTranslator(sqlExpressionFactory),
            // CONVERT(varchar(N), value) works in VistaDB for primitive .ToString() calls,
            // e.g. byte.ToString() after byte[].First() = CONVERT(varchar(3), CAST(SUBSTRING(Photo,1,1) AS tinyint)).
            new VistaDBObjectToStringTranslator(sqlExpressionFactory, typeMappingSource)
        ]);

        // VistaDB: no analog — the following SqlServer translators are deliberately not registered:
        //   * SqlServerByteArrayMethodTranslator — depends on DATALENGTH/SUBSTRING combinatorics for varbinary(max);
        //     the relational base handles ElementAt for byte arrays via VistaDBSqlTranslatingExpressionVisitor.
        //   * SqlServerConvertTranslator — wraps SqlServerDbFunctionsExtensions.Convert; VistaDB has no Convert surface.
        //   * SqlServerDataLengthFunctionTranslator — wraps SqlServerDbFunctionsExtensions.DataLength; no analog yet.
        //   * SqlServerDateDiffFunctionsTranslator — wraps SqlServerDbFunctionsExtensions.DateDiff*/DateDiffBig*;
        //     VistaDB supports DATEDIFF (including MICROSECOND and NANOSECOND) but rejects DATEDIFF_BIG with
        //     VistaDBStrings.DateDiffBigNotSupported. Add a VistaDBDbFunctionsExtensions and a sibling translator
        //     when the EF.Functions surface is published; until then there is nothing to translate.
        //   * SqlServerDateOnlyMethodTranslator / SqlServerTimeOnlyMethodTranslator — relies on DATEFROMPARTS /
        //     TIMEFROMPARTS, which VistaDB does not provide (VistaDBStrings.DateFromPartsNotSupported).
        //   * SqlServerDateTimeMethodTranslator — relies on AT TIME ZONE / DATEADD over datetimeoffset.
        //     VistaDB has no AT TIME ZONE (VistaDBStrings.AtTimeZoneNotSupported) and no datetimeoffset.
        //   * SqlServerFromPartsFunctionTranslator — *FROMPARTS family; not supported by VistaDB.
        //   * SqlServerFullTextSearchFunctionsTranslator — CONTAINS/FREETEXT; not supported by VistaDB
        //     (VistaDBStrings.FullTextSearchNotSupported).
        //   * SqlServerIsDateFunctionTranslator / SqlServerIsNumericFunctionTranslator — wrap ISDATE/ISNUMERIC via
        //     SqlServerDbFunctionsExtensions; no analog yet.
        //   * SqlServerObjectToStringTranslator — replaced by VistaDBObjectToStringTranslator registered above.
        //   * SqlServerVectorTranslator — no vector type on VistaDB (VistaDBStrings.VectorTypeNotSupported).
        // Original SqlServer registrations preserved below for future revival when VistaDB or VistaDBDbFunctionsExtensions adds support.
        /*
            new SqlServerByteArrayMethodTranslator(sqlExpressionFactory),
            new SqlServerConvertTranslator(sqlExpressionFactory),
            new SqlServerDataLengthFunctionTranslator(sqlExpressionFactory),
            new SqlServerDateDiffFunctionsTranslator(sqlExpressionFactory),
            new SqlServerDateOnlyMethodTranslator(sqlExpressionFactory),
            new SqlServerDateTimeMethodTranslator(sqlExpressionFactory, typeMappingSource),
            new SqlServerFromPartsFunctionTranslator(sqlExpressionFactory, typeMappingSource),
            new SqlServerFullTextSearchFunctionsTranslator(sqlExpressionFactory),
            new SqlServerIsDateFunctionTranslator(sqlExpressionFactory),
            new SqlServerIsNumericFunctionTranslator(sqlExpressionFactory),
            new SqlServerObjectToStringTranslator(sqlExpressionFactory, typeMappingSource),
            new SqlServerTimeOnlyMethodTranslator(sqlExpressionFactory),
            new SqlServerVectorTranslator(sqlExpressionFactory, typeMappingSource),
        */
    }
}
