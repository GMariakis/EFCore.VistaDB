// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

/// <summary>
///     Detects the exceptions caused by VistaDB transient failures.
/// </summary>
/// <remarks>
///     VistaDB is a local file-based engine, so there are no transient failures: this implementation
///     always returns false. The original SqlServer body is preserved below for future revival.
/// </remarks>
public static class VistaDBTransientExceptionDetector
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static bool ShouldRetryOn(Exception? ex)
        => false;

    // VistaDB: no analog — VistaDB has no transient errors (file-based engine, no remote service to retry against).
    // Original SqlServer logic preserved below for future revival.
    /*
        public static bool ShouldRetryOn(Exception? ex)
        {
            if (ex is SqlException sqlException)
            {
                foreach (SqlError err in sqlException.Errors)
                {
                    switch (err.Number)
                    {
                        // (extensive list of SQL Server transient error codes 49983, 49977, 49920, ..., 64, 20)
                        // truncated here for brevity; see SqlServerTransientExceptionDetector.cs in the
                        // SqlServer provider for the complete catalogue.
                        ...
                            return true;
                        case 203:
                            if (ex.InnerException is Win32Exception)
                            {
                                return true;
                            }
                            continue;
                        ...
                    }
                }

                return false;
            }

            return ex is TimeoutException;
        }
    */
}
