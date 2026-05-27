// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Microsoft.EntityFrameworkCore.VistaDB.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Infrastructure;
using Microsoft.EntityFrameworkCore.VistaDB.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.VistaDB.Metadata.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Migrations;
using Microsoft.EntityFrameworkCore.VistaDB.Migrations.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Query.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Update.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.ValueGeneration.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     VistaDB-specific extension methods for <see cref="IServiceCollection" />.
/// </summary>
public static class VistaDBServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the given Entity Framework <see cref="DbContext" /> as a service in the <see cref="IServiceCollection" />
    ///     and configures it to connect to a VistaDB database.
    /// </summary>
    /// <typeparam name="TContext">The type of context to be registered.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="connectionString">The connection string of the database to connect to.</param>
    /// <param name="vistaDbOptionsAction">An optional action to allow additional VistaDB specific configuration.</param>
    /// <param name="optionsAction">An optional action to configure the <see cref="DbContextOptions" /> for the context.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddVistaDB<TContext>(
        this IServiceCollection serviceCollection,
        string? connectionString,
        Action<VistaDBDbContextOptionsBuilder>? vistaDbOptionsAction = null,
        Action<DbContextOptionsBuilder>? optionsAction = null)
        where TContext : DbContext
        => serviceCollection.AddDbContext<TContext>(
            (_, options) =>
            {
                optionsAction?.Invoke(options);
                options.UseVistaDB(connectionString, vistaDbOptionsAction);
            });

    /// <summary>
    ///     <para>
    ///         Adds the services required by the VistaDB database provider for Entity Framework
    ///         to an <see cref="IServiceCollection" />.
    ///     </para>
    ///     <para>
    ///         Warning: Do not call this method accidentally. It is much more likely you need
    ///         to call <see cref="AddVistaDB{TContext}" />.
    ///     </para>
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IServiceCollection AddEntityFrameworkVistaDB(this IServiceCollection serviceCollection)
    {
        new EntityFrameworkRelationalServicesBuilder(serviceCollection)
            .TryAdd<LoggingDefinitions, VistaDBLoggingDefinitions>()
            .TryAdd<IDatabaseProvider, DatabaseProvider<VistaDBOptionsExtension>>()
            .TryAdd<IValueGeneratorCache>(p => p.GetRequiredService<IVistaDBValueGeneratorCache>())
            .TryAdd<IRelationalTypeMappingSource, VistaDBTypeMappingSource>()
            .TryAdd<ISqlGenerationHelper, VistaDBSqlGenerationHelper>()
            .TryAdd<IRelationalAnnotationProvider, VistaDBAnnotationProvider>()
            .TryAdd<IMigrationsAnnotationProvider, VistaDBMigrationsAnnotationProvider>()
            .TryAdd<IModelValidator, VistaDBModelValidator>()
            .TryAdd<IProviderConventionSetBuilder, VistaDBConventionSetBuilder>()
            .TryAdd<IUpdateSqlGenerator>(p => p.GetRequiredService<IVistaDBUpdateSqlGenerator>())
            .TryAdd<IEvaluatableExpressionFilter, VistaDBEvaluatableExpressionFilter>()
            .TryAdd<IRelationalTransactionFactory, VistaDBTransactionFactory>()
            .TryAdd<IModificationCommandBatchFactory, VistaDBModificationCommandBatchFactory>()
            .TryAdd<IModificationCommandFactory, VistaDBModificationCommandFactory>()
            .TryAdd<IValueGeneratorSelector, VistaDBValueGeneratorSelector>()
            .TryAdd<IRelationalConnection>(p => p.GetRequiredService<IVistaDBConnection>())
            .TryAdd<IMigrationsSqlGenerator, VistaDBMigrationsSqlGenerator>()
            .TryAdd<IRelationalDatabaseCreator, VistaDBDatabaseCreator>()
            .TryAdd<IHistoryRepository, VistaDBHistoryRepository>()
            .TryAdd<IExecutionStrategyFactory, VistaDBExecutionStrategyFactory>()
            .TryAdd<IRelationalQueryStringFactory, VistaDBQueryStringFactory>()
            .TryAdd<ICompiledQueryCacheKeyGenerator, VistaDBCompiledQueryCacheKeyGenerator>()
            .TryAdd<IQueryCompilationContextFactory, VistaDBQueryCompilationContextFactory>()
            .TryAdd<IMethodCallTranslatorProvider, VistaDBMethodCallTranslatorProvider>()
            .TryAdd<IAggregateMethodCallTranslatorProvider, VistaDBAggregateMethodCallTranslatorProvider>()
            .TryAdd<IMemberTranslatorProvider, VistaDBMemberTranslatorProvider>()
            .TryAdd<IQuerySqlGeneratorFactory, VistaDBQuerySqlGeneratorFactory>()
            .TryAdd<IRelationalSqlTranslatingExpressionVisitorFactory, VistaDBSqlTranslatingExpressionVisitorFactory>()
            .TryAdd<ISqlExpressionFactory, VistaDBSqlExpressionFactory>()
            .TryAdd<IQueryTranslationPostprocessorFactory, VistaDBQueryTranslationPostprocessorFactory>()
            .TryAdd<IRelationalParameterBasedSqlProcessorFactory, VistaDBParameterBasedSqlProcessorFactory>()
            .TryAdd<INavigationExpansionExtensibilityHelper, VistaDBNavigationExpansionExtensibilityHelper>()
            .TryAdd<IQueryableMethodTranslatingExpressionVisitorFactory, VistaDBQueryableMethodTranslatingExpressionVisitorFactory>()
            .TryAdd<IExceptionDetector, VistaDBExceptionDetector>()
            .TryAdd<ISingletonOptions, IVistaDBSingletonOptions>(p => p.GetRequiredService<IVistaDBSingletonOptions>())

            // VistaDB-specific Tier-2 hook: replace the default IMigrationCommandExecutor so that
            // VistaDBDdaMigrationCommand instances are routed through the VistaDB managed (DDA) API
            // instead of being executed as SQL via DbCommand.ExecuteNonQuery.
            .TryAdd<IMigrationCommandExecutor, VistaDBDdaMigrationCommandExecutor>()

            .TryAddProviderSpecificServices(
                b => b
                    .TryAddSingleton<IVistaDBSingletonOptions, VistaDBSingletonOptions>()
                    .TryAddSingleton<IVistaDBValueGeneratorCache, VistaDBValueGeneratorCache>()
                    .TryAddSingleton<IVistaDBUpdateSqlGenerator, VistaDBUpdateSqlGenerator>()
                    .TryAddSingleton<IVistaDBSequenceValueGeneratorFactory, VistaDBSequenceValueGeneratorFactory>()
                    .TryAddScoped<IVistaDBConnection, VistaDBConnection>()
                    .TryAddScoped<IVistaDBDdaAccessor, VistaDBDdaAccessor>())
            .TryAddCoreServices();

        return serviceCollection;
    }
}
