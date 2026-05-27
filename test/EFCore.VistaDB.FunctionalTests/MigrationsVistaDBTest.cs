// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     Smoke test for the migrations pipeline against a real VistaDB engine.
///     The test applies two migration steps sequentially (CREATE TABLE; ADD COLUMN), then reverts the
///     second step, verifying the table shape via <c>INFORMATION_SCHEMA</c> at each stage.
/// </summary>
public class MigrationsVistaDBTest
{
    [VistaDBInstalledFact(Skip = "Scaffolded migrations require generated *.Designer.cs files; revisit once the design-time tooling lands.")]
    public void Apply_and_revert_migration_updates_schema()
    {
        // TODO(EFCore.VistaDB): once the design-time tooling lands and we can generate Migration types
        // for a Person { Id, Name } -> Person { Id, Name, Email } sequence, drive the migrator and verify
        // INFORMATION_SCHEMA.COLUMNS reflects each step. For now this fact is skipped to keep the project
        // compiling without hand-rolled Migration snapshots.
    }
}
