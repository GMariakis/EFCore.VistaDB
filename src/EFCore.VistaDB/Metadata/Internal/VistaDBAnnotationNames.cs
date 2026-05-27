// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Metadata.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public static class VistaDBAnnotationNames
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public const string Prefix = "VistaDB:";

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public const string Identity = Prefix + "Identity";

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public const string IdentitySeed = Prefix + "IdentitySeed";

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public const string IdentityIncrement = Prefix + "IdentityIncrement";

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public const string ValueGenerationStrategy = Prefix + "ValueGenerationStrategy";

    // VistaDB: no analog — VistaDB has no clustered/non-clustered index distinction, no FillFactor, no
    // Online index creation, no DataCompression, no SortInTempDb, no Sparse columns, no Include columns,
    // no Memory-Optimized tables, no Temporal tables, no HiLo/Sequence support, no Azure SQL edition
    // options (MaxDatabaseSize/ServiceTier/PerformanceLevel), and no OUTPUT clause.
    // Original SqlServer annotation names preserved below for future revival when VistaDB adds support.
    /*
    public const string Clustered = Prefix + "Clustered";
    public const string CreatedOnline = Prefix + "Online";
    public const string EditionOptions = Prefix + "EditionOptions";
    public const string FillFactor = Prefix + "FillFactor";
    public const string SortInTempDb = Prefix + "SortInTempDb";
    public const string DataCompression = Prefix + "DataCompression";
    public const string HiLoSequenceName = Prefix + "HiLoSequenceName";
    public const string HiLoSequenceSchema = Prefix + "HiLoSequenceSchema";
    public const string SequenceNameSuffix = Prefix + "SequenceNameSuffix";
    public const string SequenceName = Prefix + "SequenceName";
    public const string SequenceSchema = Prefix + "SequenceSchema";
    public const string Include = Prefix + "Include";
    public const string MaxDatabaseSize = Prefix + "DatabaseMaxSize";
    public const string MemoryOptimized = Prefix + "MemoryOptimized";
    public const string PerformanceLevelSql = Prefix + "PerformanceLevelSql";
    public const string ServiceTierSql = Prefix + "ServiceTierSql";
    public const string Sparse = Prefix + "Sparse";
    public const string IsTemporal = Prefix + "IsTemporal";
    public const string TemporalHistoryTableName = Prefix + "TemporalHistoryTableName";
    public const string TemporalHistoryTableSchema = Prefix + "TemporalHistoryTableSchema";
    public const string TemporalPeriodStartPropertyName = Prefix + "TemporalPeriodStartPropertyName";
    public const string TemporalPeriodStartColumnName = Prefix + "TemporalPeriodStartColumnName";
    public const string TemporalPeriodEndPropertyName = Prefix + "TemporalPeriodEndPropertyName";
    public const string TemporalPeriodEndColumnName = Prefix + "TemporalPeriodEndColumnName";
    public const string TemporalOperationType = Prefix + "TemporalOperationType";
    public const string TemporalAsOfPointInTime = Prefix + "TemporalAsOfPointInTime";
    public const string TemporalRangeOperationFrom = Prefix + "TemporalRangeOperationFrom";
    public const string TemporalRangeOperationTo = Prefix + "TemporalRangeOperationTo";
    public const string UseSqlOutputClause = Prefix + "UseSqlOutputClause";
    public const string TemporalIsPeriodStartColumn = Prefix + "TemporalIsPeriodStartColumn";
    public const string TemporalIsPeriodEndColumn = Prefix + "TemporalIsPeriodEndColumn";
    */
}
