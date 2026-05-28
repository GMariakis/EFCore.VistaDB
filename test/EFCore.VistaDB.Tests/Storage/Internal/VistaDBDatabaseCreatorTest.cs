// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ReSharper disable UnassignedGetOnlyAutoProperty
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable MemberCanBePrivate.Local
namespace Microsoft.EntityFrameworkCore.Storage.Internal;

/// <summary>
///     Unit-test surface for <c>VistaDBDatabaseCreator</c>. The SqlServer parallel exercises
///     retry-on-transient-error semantics around <c>CREATE DATABASE</c>, which VistaDB does not need:
///     <list type="bullet">
///         <item><c>Exists()</c> is just <c>File.Exists(GetDatabaseFileName())</c>.</item>
///         <item><c>Create()</c> opens DDA via <c>VistaDBEngine.Connections.OpenDDA()</c> and calls
///             <c>CreateDatabase</c>. There are no transient errors to retry on.</item>
///         <item><c>Delete()</c> closes the connection and calls <c>File.Delete</c>.</item>
///     </list>
///     Live-engine assertions are exercised by <c>EFCore.VistaDB.FunctionalTests</c>; the cases below
///     that would otherwise require the native engine are skipped.
/// </summary>
public class VistaDBDatabaseCreatorTest
{
    // VistaDB: no analog — VistaDB has no transient errors and no CREATE DATABASE retry loop.
    // Original SqlServer test surface preserved below for future revival when VistaDB grows analogs.
    /*
    [ConditionalFact]
    public Task Create_checks_for_existence_and_retries_if_no_proccess_until_it_passes()
        => Create_checks_for_existence_and_retries_until_it_passes(233, async: false);

    [ConditionalFact]
    public Task CreateAsync_checks_for_existence_and_retries_if_no_proccess_until_it_passes()
        => Create_checks_for_existence_and_retries_until_it_passes(233, async: true);

    // ... and equivalents for SqlServer error codes -2, 4060, 1832, 5120
    */

    [ConditionalFact(Skip = "Requires live VistaDB engine — Exists() probes the .vdb6 file via VistaDBEngine.Connections.OpenDDA. Covered by EFCore.VistaDB.FunctionalTests.")]
    public void Exists_returns_false_for_missing_file()
    {
        // TODO(EFCore.VistaDB.Tests): once a sandbox without the engine is available, assert
        // creator.Exists() == false for a temp path that does not exist.
    }

    [ConditionalFact(Skip = "Requires live VistaDB engine — Create() calls native VistaDBEngine.Connections.OpenDDA().CreateDatabase. Covered by EFCore.VistaDB.FunctionalTests.")]
    public Task Create_emits_DDA_CreateDatabase_for_target_file_path()
    {
        // TODO(EFCore.VistaDB.Tests): once the engine is available, assert
        // creator.Create() produces a non-zero-length .vdb6 file at the target path
        // and that creator.Exists() flips to true.
        return Task.CompletedTask;
    }

    [ConditionalFact(Skip = "Requires live VistaDB engine — Delete() removes the .vdb6 file via File.Delete after closing DDA. Covered by EFCore.VistaDB.FunctionalTests.")]
    public Task Delete_removes_the_database_file()
    {
        // TODO(EFCore.VistaDB.Tests): create a temp file via Create(), call Delete(), assert
        // File.Exists(target) == false.
        return Task.CompletedTask;
    }
}
