// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Infrastructure;
using Microsoft.EntityFrameworkCore.VistaDB.Infrastructure.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     VistaDB-specific extension methods for <see cref="DbContextOptionsBuilder" />.
/// </summary>
public static class VistaDBDbContextOptionsExtensions
{
    /// <summary>
    ///     Configures the context to connect to a VistaDB database, but without initially setting any
    ///     <see cref="DbConnection" /> or connection string.
    /// </summary>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="vistaDbOptionsAction">An optional action to allow additional VistaDB specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder UseVistaDB(
        this DbContextOptionsBuilder optionsBuilder,
        Action<VistaDBDbContextOptionsBuilder>? vistaDbOptionsAction = null)
    {
        var extension = GetOrCreateExtension(optionsBuilder);
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
        return ApplyConfiguration(optionsBuilder, vistaDbOptionsAction);
    }

    /// <summary>
    ///     Configures the context to connect to a VistaDB database.
    /// </summary>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connectionString">The connection string of the database to connect to.</param>
    /// <param name="vistaDbOptionsAction">An optional action to allow additional VistaDB specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder UseVistaDB(
        this DbContextOptionsBuilder optionsBuilder,
        string? connectionString,
        Action<VistaDBDbContextOptionsBuilder>? vistaDbOptionsAction = null)
    {
        var extension = (VistaDBOptionsExtension)GetOrCreateExtension(optionsBuilder).WithConnectionString(connectionString);
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
        return ApplyConfiguration(optionsBuilder, vistaDbOptionsAction);
    }

    /// <summary>
    ///     Configures the context to connect to a VistaDB database.
    /// </summary>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connection">
    ///     An existing <see cref="DbConnection" /> to be used to connect to the database. If the connection is
    ///     in the open state then EF will not open or close the connection. If the connection is in the closed
    ///     state then EF will open and close the connection as needed. The caller owns the connection and is
    ///     responsible for its disposal.
    /// </param>
    /// <param name="vistaDbOptionsAction">An optional action to allow additional VistaDB specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder UseVistaDB(
        this DbContextOptionsBuilder optionsBuilder,
        DbConnection connection,
        Action<VistaDBDbContextOptionsBuilder>? vistaDbOptionsAction = null)
        => UseVistaDB(optionsBuilder, connection, contextOwnsConnection: false, vistaDbOptionsAction);

    /// <summary>
    ///     Configures the context to connect to a VistaDB database.
    /// </summary>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connection">
    ///     An existing <see cref="DbConnection" /> to be used to connect to the database.
    /// </param>
    /// <param name="contextOwnsConnection">
    ///     If <see langword="true" />, then EF will take ownership of the connection and dispose it.
    /// </param>
    /// <param name="vistaDbOptionsAction">An optional action to allow additional VistaDB specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder UseVistaDB(
        this DbContextOptionsBuilder optionsBuilder,
        DbConnection connection,
        bool contextOwnsConnection,
        Action<VistaDBDbContextOptionsBuilder>? vistaDbOptionsAction = null)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var extension = (VistaDBOptionsExtension)GetOrCreateExtension(optionsBuilder).WithConnection(connection, contextOwnsConnection);
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
        return ApplyConfiguration(optionsBuilder, vistaDbOptionsAction);
    }

    /// <summary>
    ///     Configures the context to connect to a VistaDB database, but without initially setting any
    ///     <see cref="DbConnection" /> or connection string.
    /// </summary>
    /// <typeparam name="TContext">The type of context to be configured.</typeparam>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="vistaDbOptionsAction">An optional action to allow additional VistaDB specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder<TContext> UseVistaDB<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        Action<VistaDBDbContextOptionsBuilder>? vistaDbOptionsAction = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseVistaDB(
            (DbContextOptionsBuilder)optionsBuilder, vistaDbOptionsAction);

    /// <summary>
    ///     Configures the context to connect to a VistaDB database.
    /// </summary>
    /// <typeparam name="TContext">The type of context to be configured.</typeparam>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connectionString">The connection string of the database to connect to.</param>
    /// <param name="vistaDbOptionsAction">An optional action to allow additional VistaDB specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder<TContext> UseVistaDB<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        string? connectionString,
        Action<VistaDBDbContextOptionsBuilder>? vistaDbOptionsAction = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseVistaDB(
            (DbContextOptionsBuilder)optionsBuilder, connectionString, vistaDbOptionsAction);

    /// <summary>
    ///     Configures the context to connect to a VistaDB database.
    /// </summary>
    /// <typeparam name="TContext">The type of context to be configured.</typeparam>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connection">An existing <see cref="DbConnection" /> to be used to connect to the database.</param>
    /// <param name="vistaDbOptionsAction">An optional action to allow additional VistaDB specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder<TContext> UseVistaDB<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        DbConnection connection,
        Action<VistaDBDbContextOptionsBuilder>? vistaDbOptionsAction = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseVistaDB(
            (DbContextOptionsBuilder)optionsBuilder, connection, vistaDbOptionsAction);

    /// <summary>
    ///     Configures the context to connect to a VistaDB database.
    /// </summary>
    /// <typeparam name="TContext">The type of context to be configured.</typeparam>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connection">An existing <see cref="DbConnection" /> to be used to connect to the database.</param>
    /// <param name="contextOwnsConnection">If <see langword="true" />, EF takes ownership of the connection.</param>
    /// <param name="vistaDbOptionsAction">An optional action to allow additional VistaDB specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder<TContext> UseVistaDB<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        DbConnection connection,
        bool contextOwnsConnection,
        Action<VistaDBDbContextOptionsBuilder>? vistaDbOptionsAction = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseVistaDB(
            (DbContextOptionsBuilder)optionsBuilder, connection, contextOwnsConnection, vistaDbOptionsAction);

    private static VistaDBOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.Options.FindExtension<VistaDBOptionsExtension>()
            ?? new VistaDBOptionsExtension();

    private static DbContextOptionsBuilder ApplyConfiguration(
        DbContextOptionsBuilder optionsBuilder,
        Action<VistaDBDbContextOptionsBuilder>? vistaDbOptionsAction)
    {
        ConfigureWarnings(optionsBuilder);

        vistaDbOptionsAction?.Invoke(new VistaDBDbContextOptionsBuilder(optionsBuilder));

        // VistaDB has no retry-by-default or other engine-specific options to fold in, so no
        // ApplyDefaults pass is needed here. (SqlServer uses ApplyDefaults to install the
        // SqlServerRetryingExecutionStrategy when EngineType is Azure SQL / Azure Synapse — VistaDB has
        // no Azure variant and is file-based with no transient errors, so this is a no-op.)
        var extension = GetOrCreateExtension(optionsBuilder);
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

        return optionsBuilder;
    }

    private static void ConfigureWarnings(DbContextOptionsBuilder optionsBuilder)
    {
        var coreOptionsExtension
            = optionsBuilder.Options.FindExtension<CoreOptionsExtension>()
            ?? new CoreOptionsExtension();

        coreOptionsExtension = RelationalOptionsExtension.WithDefaultWarningConfiguration(coreOptionsExtension);

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(coreOptionsExtension);
    }
}
