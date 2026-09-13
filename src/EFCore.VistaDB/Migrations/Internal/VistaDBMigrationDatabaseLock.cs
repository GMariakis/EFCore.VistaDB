// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Migrations.Internal;

/// <summary>
///     Serializes cooperating EF migrators through an exclusively opened sidecar file.
///     Ordinary VistaDB connections and external editors retain their normal engine locking.
/// </summary>
/// <remarks>
///     The sidecar remains on disk after release: deleting it can let two waiters acquire
///     different file objects. Ownership is the open OS handle, not the existence of the file,
///     so process termination releases the lock without stale-owner cleanup.
/// </remarks>
public sealed class VistaDBMigrationDatabaseLock : IMigrationsDatabaseLock
{
    private FileStream? _handle;

    private VistaDBMigrationDatabaseLock(IHistoryRepository historyRepository, FileStream handle)
    {
        HistoryRepository = historyRepository;
        _handle = handle;
    }

    public IHistoryRepository HistoryRepository { get; }

    internal static async ValueTask<VistaDBMigrationDatabaseLock> Acquire(
        IHistoryRepository historyRepository, string databasePath, int timeoutSeconds, bool async,
        CancellationToken cancellationToken = default)
    {
        var databaseFile = new FileInfo(Path.GetFullPath(databasePath));
        // Follow a database-file symlink. Parent-directory junctions are handled by the OS
        // when the adjacent lock file is opened. Hard-link aliases require a shared canonical path.
        var canonicalPath = databaseFile.LinkTarget is null
            ? databaseFile.FullName
            : databaseFile.ResolveLinkTarget(returnFinalTarget: true)!.FullName;
        var lockPath = canonicalPath + ".efcore-migrations.lock";
        var elapsed = Stopwatch.StartNew();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return new VistaDBMigrationDatabaseLock(
                    historyRepository, new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
            }
            catch (IOException exception) when ((exception.HResult & 0xffff) is 32 or 33 or 11)
            {
                // Retry sharing/locking contention, not path, permission or disk failures.
                if (timeoutSeconds > 0 && elapsed.Elapsed >= TimeSpan.FromSeconds(timeoutSeconds))
                {
                    throw new TimeoutException($"Timed out acquiring the VistaDB migration lock for '{canonicalPath}'.", exception);
                }
            }

            if (async)
            {
                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                Thread.Sleep(50);
            }
        }
    }

    public void Dispose()
        => Interlocked.Exchange(ref _handle, null)?.Dispose();

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _handle, null) is { } handle)
        {
            await handle.DisposeAsync().ConfigureAwait(false);
        }
    }
}
