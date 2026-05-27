// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Metadata.Conventions;

/// <summary>
///     A builder for building conventions for VistaDB.
/// </summary>
/// <remarks>
///     <para>
///         The service lifetime is <see cref="ServiceLifetime.Scoped" /> and multiple registrations
///         are allowed. This means that each <see cref="DbContext" /> instance will use its own
///         set of instances of this service.
///     </para>
/// </remarks>
public class VistaDBConventionSetBuilder : RelationalConventionSetBuilder
{
    private readonly ISqlGenerationHelper _sqlGenerationHelper;

    /// <summary>
    ///     Creates a new <see cref="VistaDBConventionSetBuilder" /> instance.
    /// </summary>
    /// <param name="dependencies">The core dependencies for this service.</param>
    /// <param name="relationalDependencies">The relational dependencies for this service.</param>
    /// <param name="sqlGenerationHelper">The SQL generation helper to use.</param>
    public VistaDBConventionSetBuilder(
        ProviderConventionSetBuilderDependencies dependencies,
        RelationalConventionSetBuilderDependencies relationalDependencies,
        ISqlGenerationHelper sqlGenerationHelper)
        : base(dependencies, relationalDependencies)
        => _sqlGenerationHelper = sqlGenerationHelper;

    /// <summary>
    ///     Builds and returns the convention set for the current database provider.
    /// </summary>
    /// <returns>The convention set for the current database provider.</returns>
    public override ConventionSet CreateConventionSet()
    {
        var conventionSet = base.CreateConventionSet();

        // VistaDB max identifier length is 128 (same as SQL Server).
        conventionSet.Add(new RelationalMaxIdentifierLengthConvention(128, Dependencies, RelationalDependencies));

        // Default the model's value-generation strategy to IdentityColumn and propagate it to integer
        // PK properties at model-finalize time. Without this convention, EF Core's relational base
        // never stamps the VistaDB:ValueGenerationStrategy annotation, the migrations SQL generator
        // never emits IDENTITY(1,1), and every INSERT that doesn't supply a PK value fails with
        // VistaDB Error 174 ("Column cannot contain null value: Id"). Mirrors
        // SqlServerValueGenerationStrategyConvention.
        conventionSet.Add(new VistaDBValueGenerationStrategyConvention(Dependencies, RelationalDependencies));

        conventionSet.Replace<ValueGenerationConvention>(
            new VistaDBValueGenerationConvention(Dependencies, RelationalDependencies));

        // Detect circular FK cascade paths on shared join tables and convert DeleteBehavior.Cascade
        // to ClientCascade so the migration generator and spec tests see the correct behavior.
        // Mirrors SqlServerConventionSetBuilder — VistaDB shares the same self-referencing skip-nav
        // table constraint pattern.
        conventionSet.Replace<CascadeDeleteConvention>(
            new VistaDBOnDeleteConvention(Dependencies, RelationalDependencies));

        // VistaDB: no analog — conventions for SqlServer features VistaDB does not support
        // (memory-optimized tables, OUTPUT clause, temporal tables, custom DbFunctions handling,
        // clustered/online/include-column indexes, named default-constraints) are not registered.
        // Original SqlServer convention registrations preserved below for future revival.
        /*
        conventionSet.Add(new SqlServerValueGenerationStrategyConvention(Dependencies, RelationalDependencies));
        conventionSet.Add(new SqlServerIndexConvention(Dependencies, RelationalDependencies, _sqlGenerationHelper));
        conventionSet.Add(new SqlServerMemoryOptimizedTablesConvention(Dependencies, RelationalDependencies));
        conventionSet.Add(new SqlServerDbFunctionConvention(Dependencies, RelationalDependencies));
        conventionSet.Add(new SqlServerOutputClauseConvention(Dependencies, RelationalDependencies));

        conventionSet.Replace<CascadeDeleteConvention>(
            new VistaDBOnDeleteConvention(Dependencies, RelationalDependencies));
        conventionSet.Replace<StoreGenerationConvention>(
            new SqlServerStoreGenerationConvention(Dependencies, RelationalDependencies));
        conventionSet.Replace<RuntimeModelConvention>(new SqlServerRuntimeModelConvention(Dependencies, RelationalDependencies));
        conventionSet.Replace<SharedTableConvention>(
            new SqlServerSharedTableConvention(Dependencies, RelationalDependencies));

        var sqlServerTemporalConvention = new SqlServerTemporalConvention(Dependencies, RelationalDependencies);
        ConventionSet.AddBefore(
            conventionSet.EntityTypeAnnotationChangedConventions,
            sqlServerTemporalConvention,
            typeof(SqlServerValueGenerationConvention));
        conventionSet.SkipNavigationForeignKeyChangedConventions.Add(sqlServerTemporalConvention);
        conventionSet.ModelFinalizingConventions.Add(sqlServerTemporalConvention);
        */

        // Reference _sqlGenerationHelper to avoid CS0414 once the SqlServerIndexConvention block is revived.
        _ = _sqlGenerationHelper;

        return conventionSet;
    }

    /// <summary>
    ///     Call this method to build a <see cref="ConventionSet" /> for VistaDB when using
    ///     the <see cref="ModelBuilder" /> outside of <see cref="DbContext.OnModelCreating" />.
    /// </summary>
    /// <returns>The convention set.</returns>
    public static ConventionSet Build()
    {
        // VistaDB: no analog — a CreateServiceScope helper that calls AddEntityFrameworkVistaDB will be added
        // alongside ServiceCollectionExtensions in a later task. For now, throw to make the missing wiring loud.
        throw new NotSupportedException(
            "VistaDBConventionSetBuilder.Build requires the VistaDB service collection extensions, which are not yet implemented.");
    }

    /// <summary>
    ///     Call this method to build a <see cref="ModelBuilder" /> for VistaDB outside of <see cref="DbContext.OnModelCreating" />.
    /// </summary>
    /// <returns>The convention set.</returns>
    public static ModelBuilder CreateModelBuilder()
    {
        throw new NotSupportedException(
            "VistaDBConventionSetBuilder.CreateModelBuilder requires the VistaDB service collection extensions, which are not yet implemented.");
    }
}
