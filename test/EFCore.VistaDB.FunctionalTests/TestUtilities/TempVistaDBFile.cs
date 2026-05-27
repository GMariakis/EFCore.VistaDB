// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.EntityFrameworkCore.TestUtilities;

/// <summary>
///     RAII wrapper that produces a unique temporary .vdb6 file path on construction and deletes the file
///     (and any associated lock files) on <see cref="Dispose" />. Intended for VistaDB integration tests
///     that want to use <c>EnsureCreated</c> / <c>EnsureDeleted</c> against a real engine without leaking
///     leftover files in <c>%TEMP%</c>.
/// </summary>
public sealed class TempVistaDBFile : IDisposable
{
    public TempVistaDBFile()
    {
        FilePath = Path.Combine(Path.GetTempPath(), $"efcore-vistadb-test-{Guid.NewGuid():N}.vdb6");
        ConnectionString = $"Data Source={FilePath}";
    }

    public string FilePath { get; }

    public string ConnectionString { get; }

    public bool Exists
        => File.Exists(FilePath);

    public void Dispose()
    {
        TryDelete(FilePath);
        TryDelete(FilePath + ".lock");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best effort: the OS may still hold a handle. The file lives in %TEMP% so it's not critical.
        }
    }
}
