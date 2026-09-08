// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;
using VistaDB;

namespace Microsoft.EntityFrameworkCore.Storage;

/// <summary>
///     How a <c>.vdb6</c> gets opened.
///
///     The engine requires every handle on a file to be in the same locking family, and offers no way
///     to ask which family is already in force. Measured against VistaDB 6.6.2: MultiProcess beside
///     MultiProcess is fine, in one process or across two; SingleProcess beside SingleProcess is fine
///     within one process; MultiProcess beside SingleProcess fails either way round, with error 218
///     naming whichever handle asked for SingleProcess.
///
///     The provider used to have three openers each hardcoding a mode and each carrying a comment
///     claiming the others matched. Two of those comments were wrong and the connection and the DDA
///     handle had drifted into the one pairing that cannot work. These tests pin the rule that replaced
///     them: every mode is derived from the connection string, so they cannot drift again.
/// </summary>
public class VistaDBOpenModesTest
{
    private const string Path = "Data Source=C:\\db\\gate.vdb6;";

    [ConditionalFact]
    public void Multi_process_is_the_default_when_the_connection_string_names_no_mode()
        // The permissive choice: SingleProcess means "shared inside this process and nowhere else",
        // which locks out tooling and a second application for no benefit.
        => Assert.Equal(VistaDBDatabaseOpenMode.MultiProcessReadWrite, VistaDBOpenModes.FromConnectionString(Path));

    [ConditionalTheory]
    [InlineData(null)]
    [InlineData("")]
    public void Nothing_at_all_is_still_the_default(string? connectionString)
        => Assert.Equal(VistaDBDatabaseOpenMode.MultiProcessReadWrite, VistaDBOpenModes.FromConnectionString(connectionString));

    [ConditionalFact]
    public void A_dda_handle_defaults_to_the_family_the_connection_defaults_to()
    {
        // This is the regression. The DDA handle hardcoded SingleProcessReadWrite while the connection
        // was augmented to MultiProcessReadWrite, and the engine refuses that pairing.
        Assert.Equal(VistaDBDatabaseOpenMode.MultiProcessReadWrite, VistaDBOpenModes.ForDda(Path, readOnly: false));
        Assert.Equal(VistaDBDatabaseOpenMode.MultiProcessReadOnly, VistaDBOpenModes.ForDda(Path, readOnly: true));
    }

    [ConditionalTheory]
    [InlineData("SingleProcessReadWrite", VistaDBDatabaseOpenMode.SingleProcessReadWrite, VistaDBDatabaseOpenMode.SingleProcessReadOnly)]
    [InlineData("MultiProcessReadWrite", VistaDBDatabaseOpenMode.MultiProcessReadWrite, VistaDBDatabaseOpenMode.MultiProcessReadOnly)]
    [InlineData("ExclusiveReadWrite", VistaDBDatabaseOpenMode.ExclusiveReadWrite, VistaDBDatabaseOpenMode.ExclusiveReadOnly)]
    public void An_explicit_mode_takes_the_dda_handle_with_it(
        string named, VistaDBDatabaseOpenMode expectedWrite, VistaDBDatabaseOpenMode expectedRead)
    {
        // A caller who asks for stricter exclusivity has to move every opener together, or they land in
        // the mismatch from the other side.
        var connectionString = Path + $"Open Mode={named};";

        Assert.Equal(expectedWrite, VistaDBOpenModes.ForDda(connectionString, readOnly: false));
        Assert.Equal(expectedRead, VistaDBOpenModes.ForDda(connectionString, readOnly: true));
    }

    [ConditionalTheory]
    [InlineData("SingleProcessReadOnly", VistaDBDatabaseOpenMode.SingleProcessReadWrite)]
    [InlineData("MultiProcessReadOnly", VistaDBDatabaseOpenMode.MultiProcessReadWrite)]
    public void A_read_only_mode_still_yields_a_writable_handle_in_the_same_family(
        string named, VistaDBDatabaseOpenMode expected)
        // Read-only is not the axis the engine cares about; the family is.
        => Assert.Equal(expected, VistaDBOpenModes.ForDda(Path + $"Open Mode={named};", readOnly: false));

    [ConditionalTheory]
    [InlineData("multiprocessreadwrite")]
    [InlineData("MULTIPROCESSREADWRITE")]
    public void The_mode_is_matched_without_regard_to_case(string named)
        => Assert.Equal(
            VistaDBDatabaseOpenMode.MultiProcessReadWrite,
            VistaDBOpenModes.FromConnectionString(Path + $"Open Mode={named};"));

    [ConditionalFact]
    public void An_unparseable_mode_falls_back_rather_than_throwing()
        // The engine will reject it on open with a message about the mode; failing here would blame the
        // provider for the user's typo and bury the real one.
        => Assert.Equal(
            VistaDBDatabaseOpenMode.MultiProcessReadWrite,
            VistaDBOpenModes.FromConnectionString(Path + "Open Mode=Nonsense;"));

    [ConditionalFact]
    public void The_keyword_is_the_one_VistaDB_actually_reads()
        // "OpenMode" without the space is not the connection-string keyword; a mismatch here would make
        // an explicit choice silently ignored and put the openers back out of step.
        => Assert.Equal("Open Mode", VistaDBOpenModes.Keyword);
}
