// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using System.IO;
using VistaDB.Provider;

#nullable enable

namespace Microsoft.EntityFrameworkCore.TestUtilities;

/// <summary>
///     Lightweight test store for VistaDB. Each store backs onto a temporary <c>.vdb6</c> file under
///     <c>%TEMP%</c>. The file is created on construction and deleted on <see cref="DisposeAsync"/>.
/// </summary>
public class VistaDBTestStore : IAsyncDisposable
{
    public const int CommandTimeout = 300;

    private readonly string _filePath;

    public string ConnectionString { get; }

    /// <summary>The logical store name (used to derive the file path for named/shared stores).</summary>
    public string Name { get; }

    private VistaDBTestStore(string name, bool uniqueSuffix = false)
    {
        Name = name;
        var safe = new string(name.Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_').ToArray());
        var suffix = uniqueSuffix ? $"-{Guid.NewGuid():N}" : string.Empty;
        _filePath = Path.Combine(Path.GetTempPath(), $"efcore-vistadb-{safe}{suffix}.vdb6");
        ConnectionString = $"Data Source={_filePath}";
    }

    /// <summary>Returns a connection string for a named store (keyed by name, no unique suffix).</summary>
    public static string CreateConnectionString(string name)
    {
        var safe = new string(name.Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_').ToArray());
        var filePath = Path.Combine(Path.GetTempPath(), $"efcore-vistadb-{safe}.vdb6");
        return $"Data Source={filePath}";
    }

    public static VistaDBTestStore Create(string name) => new(name, uniqueSuffix: true);

    public static VistaDBTestStore GetOrCreate(string name) => new(name, uniqueSuffix: false);

    public static Task<VistaDBTestStore> CreateInitializedAsync(string name)
    {
        // Use uniqueSuffix: false so the file path matches what CreateConnectionString(name)
        // produces — both derive the path from the name alone, no GUID suffix.
        var store = new VistaDBTestStore(name, uniqueSuffix: false);
        // Clear the VistaDB engine's static connection cache so File.Delete can remove any
        // previously open file (the engine holds handles even after Close()).
        try { global::VistaDB.DDA.VistaDBEngine.Connections.Clear(); } catch { }
        // Delete any leftover file from a previous run so each test starts clean.
        TryDelete(store._filePath);
        TryDelete(store._filePath + ".lock");
        return Task.FromResult(store);
    }

    public int ExecuteNonQuery(string sql, params object[] parameters)
    {
        using var conn = new VistaDBConnection(ConnectionString);
        if (conn.State != ConnectionState.Open) conn.Open();
        using var command = (VistaDBCommand)conn.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = CommandTimeout;
        if (parameters != null)
            for (var i = 0; i < parameters.Length; i++)
                command.Parameters.AddWithValue("@p" + i, parameters[i]);
        return command.ExecuteNonQuery();
    }

    public T ExecuteScalar<T>(string sql, params object[] parameters)
    {
        using var conn = new VistaDBConnection(ConnectionString);
        if (conn.State != ConnectionState.Open) conn.Open();
        using var command = (VistaDBCommand)conn.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = CommandTimeout;
        if (parameters != null)
            for (var i = 0; i < parameters.Length; i++)
                command.Parameters.AddWithValue("@p" + i, parameters[i]);
        return (T)command.ExecuteScalar()!;
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Yield();
        TryDelete(_filePath);
        TryDelete(_filePath + ".lock");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Best-effort; %TEMP% file
        }
    }
}
