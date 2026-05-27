// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using VistaDB;
using VistaDB.DDA;

namespace Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBDdaAccessor : IVistaDBDdaAccessor
{
    private readonly IVistaDBConnection _connection;
    private IVistaDBDDA? _dda;
    private bool _disposed;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBDdaAccessor(IVistaDBConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual IVistaDBDDA Dda
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _dda ??= VistaDBEngine.Connections.OpenDDA();
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual bool IsResolved
        => _dda is not null && !_disposed;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual string GetDatabaseFilePath()
        => ResolveDatabaseFilePath();

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual IVistaDBDatabase OpenDatabase(bool readOnly = false)
    {
        var filePath = ResolveDatabaseFilePath();
        var password = ExtractPassword(_connection.ConnectionString);

        // Use SingleProcessReadWrite (and SingleProcessReadOnly) so the DDA database handle can coexist
        // with the open ADO.NET VistaDBConnection in the same process. With MultiProcessReadWrite the
        // engine's IntraProcessLockManager rejects the second open ("Cannot open data storage or file")
        // because it treats the two opens as cross-process even though they're in the same AppDomain.
        // The ADO.NET connection's open mode is set to match in VistaDBConnection.CreateDbConnection.
        var mode = readOnly
            ? VistaDBDatabaseOpenMode.SingleProcessReadOnly
            : VistaDBDatabaseOpenMode.SingleProcessReadWrite;

        return Dda.OpenDatabase(filePath, mode, password);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual void WithDatabase(Action<IVistaDBDatabase> action, bool readOnly = false)
    {
        using var database = OpenDatabase(readOnly);
        action(database);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual void WithTable(string tableName, Action<IVistaDBTable> action, bool readOnly = false, bool exclusive = false)
    {
        using var database = OpenDatabase(readOnly);
        using var table = database.OpenTable(tableName, exclusive, readOnly);
        action(table);
    }

    private string ResolveDatabaseFilePath()
    {
        // The VistaDB ADO.NET connection's DataSource is the .vdb6 file path.
        var filePath = _connection.DbConnection.DataSource;
        if (string.IsNullOrEmpty(filePath))
        {
            // Fall back to parsing the connection string ("Data Source=...").
            var csb = new DbConnectionStringBuilder { ConnectionString = _connection.ConnectionString };
            if (csb.TryGetValue("Data Source", out var ds) && ds is string s)
            {
                filePath = s;
            }
        }

        return filePath ?? throw new InvalidOperationException(
            "VistaDB connection has no 'Data Source' (database file path) set.");
    }

    private static string? ExtractPassword(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return null;
        }

        try
        {
            var csb = new DbConnectionStringBuilder { ConnectionString = connectionString };
            if (csb.TryGetValue("Password", out var pwd) && pwd is string s && s.Length > 0)
            {
                return s;
            }
        }
        catch
        {
            // Parsing failed — fall through and return null.
        }

        return null;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_dda is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _dda = null;
        GC.SuppressFinalize(this);
    }
}
