// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBExecutionStrategy : IExecutionStrategy
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBExecutionStrategy(ExecutionStrategyDependencies dependencies)
        => Dependencies = dependencies;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual ExecutionStrategyDependencies Dependencies { get; }

    /// <summary>
    ///     VistaDB is local file-based and has no transient failures to retry on.
    /// </summary>
    public virtual bool RetriesOnFailure
        => false;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual TResult Execute<TState, TResult>(
        TState state,
        Func<DbContext, TState, TResult> operation,
        Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded)
        => operation(Dependencies.CurrentContext.Context, state);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual Task<TResult> ExecuteAsync<TState, TResult>(
        TState state,
        Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
        Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded,
        CancellationToken cancellationToken)
        => operation(Dependencies.CurrentContext.Context, state, cancellationToken);

    // VistaDB: no analog — VistaDB has no transient errors to retry. Original SqlServer body preserved
    // below for future revival.
    /*
        public virtual TResult Execute<TState, TResult>(...)
        {
            try
            {
                return operation(Dependencies.CurrentContext.Context, state);
            }
            catch (Exception ex) when (ExecutionStrategy.CallOnWrappedException(ex, SqlServerTransientExceptionDetector.ShouldRetryOn))
            {
                throw new InvalidOperationException(SqlServerStrings.TransientExceptionDetected, ex);
            }
        }
    */
}
