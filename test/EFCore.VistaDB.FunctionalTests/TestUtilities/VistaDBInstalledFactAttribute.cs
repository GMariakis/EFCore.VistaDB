// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.TestUtilities;

/// <summary>
///     xUnit fact attribute that skips when the VistaDB engine is not available on the host (e.g. CI agents
///     that don't have <c>VistaDB.6.dll</c> registered). Detection is best-effort: we try to construct a
///     <c>VistaDB.Provider.VistaDBConnection</c>; if it throws, we mark the test skipped with a friendly
///     message.
/// </summary>
public sealed class VistaDBInstalledFactAttribute : FactAttribute
{
    private static readonly bool _isInstalled;
    private static readonly string _skipReason;

    static VistaDBInstalledFactAttribute()
    {
        try
        {
            using var _ = new global::VistaDB.Provider.VistaDBConnection();
            _isInstalled = true;
        }
        catch (Exception ex)
        {
            _isInstalled = false;
            _skipReason = $"VistaDB engine is not available on this host: {ex.GetType().Name}: {ex.Message}";
        }
    }

    public override string Skip
    {
        get => _isInstalled ? base.Skip : _skipReason;
        set => base.Skip = value;
    }
}
