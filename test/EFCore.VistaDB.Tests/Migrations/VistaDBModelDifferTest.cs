// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Migrations;

/// <summary>
///     Unit-test surface for the VistaDB <c>IMigrationsModelDiffer</c>. The SqlServer parallel
///     (<c>SqlServerModelDifferTest</c>, ~2000 lines) exercises diffs that involve schemas,
///     sequences, computed columns, temporal tables, memory-optimized tables, identity options,
///     OUTPUT-clause toggles, and CHANGE_TRACKING. VistaDB does not support any of those, so the
///     corresponding diff scenarios are dropped here.
///
///     The diff scenarios that DO apply to VistaDB — CreateTable/AddColumn/AlterColumn/AddIndex/
///     AddForeignKey/RenameTable, with identity-column key handling and FK cascade emission via
///     <c>VistaDBDdaMigrationCommand</c> — are exercised end-to-end by the FunctionalTests project.
///     This unit-test file remains as a placeholder so the parallel between SqlServer and VistaDB
///     test layouts stays consistent.
/// </summary>
public class VistaDBModelDifferTest
{
    // VistaDB: no analog — VistaDB does not support schemas, sequences, computed columns, temporal
    // tables, memory-optimized tables, OUTPUT clause, identity (seed,increment) round-trip on
    // ALTER COLUMN, or CHANGE_TRACKING. The bulk of SqlServerModelDifferTest exercises diffs that
    // touch those features.
    // Original SqlServer test surface preserved on the SqlServer side for future revival when
    // VistaDB grows analogs.
    /*
    [ConditionalFact]
    public void Add_temporal_table() { ... }
    [ConditionalFact]
    public void Alter_table_change_to_temporal() { ... }
    [ConditionalFact]
    public void Add_sequence() { ... }
    [ConditionalFact]
    public void Alter_column_change_computed_sql() { ... }
    [ConditionalFact]
    public void Alter_table_change_memory_optimized() { ... }
    // ... ~80 more cases
    */

    [ConditionalFact(Skip = "Requires the full MigrationsModelDiffer dependency graph wired for VistaDB. Diff behavior is exercised end-to-end by EFCore.VistaDB.FunctionalTests; this unit-level file remains as a layout placeholder.")]
    public void Add_table_with_identity_PK_routes_FK_cascade_through_VistaDBDdaMigrationCommand()
    {
        // TODO(EFCore.VistaDB.Tests): wire MigrationsModelDifferDependencies and assert that a
        // diff producing a CreateTableOperation followed by an AddForeignKeyOperation with
        // ReferentialAction.Cascade routes through VistaDBDdaMigrationCommand (Tier-2), not a
        // plain MigrationCommand. This is the only VistaDB-specific shape worth asserting at the
        // unit level; everything else is covered by relational-base diff tests.
    }
}
