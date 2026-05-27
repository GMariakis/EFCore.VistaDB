// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Metadata.Conventions;

/// <summary>
///     A convention that configures the default model <see cref="VistaDBValueGenerationStrategy" /> as
///     <see cref="VistaDBValueGenerationStrategy.IdentityColumn" />.
/// </summary>
/// <remarks>
///     <para>
///         Direct port of <c>SqlServerValueGenerationStrategyConvention</c> at
///         <c>src/EFCore.SqlServer/Metadata/Conventions/SqlServerValueGenerationStrategyConvention.cs</c>.
///     </para>
///     <para>
///         <list type="bullet">
///             <item>
///                 <description>
///                     <see cref="ProcessModelInitialized" /> sets the model-level default strategy to
///                     <see cref="VistaDBValueGenerationStrategy.IdentityColumn" />. Without this,
///                     integer primary-key properties produce plain <c>INT NOT NULL</c> columns at
///                     CREATE TABLE time, and EF Core-generated inserts fail with VistaDB Error 174
///                     ("Column cannot contain null value: Id").
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <see cref="ProcessModelFinalizing" /> walks every property and, when the
///                     resolved per-store-object strategy is not <see cref="VistaDBValueGenerationStrategy.None" />,
///                     stamps the annotation on the property builder so the annotation provider can
///                     forward it to the column operation at migration-emit time.
///                 </description>
///             </item>
///         </list>
///     </para>
///     <para>
///         The <c>SequenceHiLo</c> / <c>Sequence</c> branches from the SqlServer original are
///         dropped because VistaDB has no <c>CREATE SEQUENCE</c>. Original SqlServer logic preserved
///         in marker block.
///     </para>
/// </remarks>
public class VistaDBValueGenerationStrategyConvention : IModelInitializedConvention, IModelFinalizingConvention
{
    /// <summary>
    ///     Creates a new <see cref="VistaDBValueGenerationStrategyConvention" /> instance.
    /// </summary>
    /// <param name="dependencies">Parameter object containing dependencies for this convention.</param>
    /// <param name="relationalDependencies">Parameter object containing relational dependencies for this convention.</param>
    public VistaDBValueGenerationStrategyConvention(
        ProviderConventionSetBuilderDependencies dependencies,
        RelationalConventionSetBuilderDependencies relationalDependencies)
    {
        Dependencies = dependencies;
        RelationalDependencies = relationalDependencies;
    }

    /// <summary>
    ///     Dependencies for this service.
    /// </summary>
    protected virtual ProviderConventionSetBuilderDependencies Dependencies { get; }

    /// <summary>
    ///     Relational provider-specific dependencies for this service.
    /// </summary>
    protected virtual RelationalConventionSetBuilderDependencies RelationalDependencies { get; }

    /// <inheritdoc />
    public virtual void ProcessModelInitialized(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
        => modelBuilder.HasValueGenerationStrategy(VistaDBValueGenerationStrategy.IdentityColumn);

    /// <inheritdoc />
    public virtual void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            foreach (var property in entityType.GetDeclaredProperties())
            {
                VistaDBValueGenerationStrategy? strategy = null;
                var declaringTable = property.GetMappedStoreObjects(StoreObjectType.Table).FirstOrDefault();
                if (declaringTable.Name != null!)
                {
                    strategy = property.GetValueGenerationStrategy(declaringTable, Dependencies.TypeMappingSource);
                    if (strategy == VistaDBValueGenerationStrategy.None
                        && !IsStrategyNoneNeeded(property, declaringTable))
                    {
                        strategy = null;
                    }
                }
                else
                {
                    var declaringView = property.GetMappedStoreObjects(StoreObjectType.View).FirstOrDefault();
                    if (declaringView.Name != null!)
                    {
                        strategy = property.GetValueGenerationStrategy(declaringView, Dependencies.TypeMappingSource);
                        if (strategy == VistaDBValueGenerationStrategy.None
                            && !IsStrategyNoneNeeded(property, declaringView))
                        {
                            strategy = null;
                        }
                    }
                }

                // Needed for the annotation to show up in the model snapshot
                if (strategy != null
                    && declaringTable.Name != null)
                {
                    property.Builder.HasValueGenerationStrategy(strategy);

                    // VistaDB: no analog — SqlServer's original here also branches on `Sequence` to attach
                    // a HiLo default-value-sql. VistaDB has no sequences; the branch is omitted.
                    // Original SqlServer logic preserved below for future revival when VistaDB adds support.
                    /*
                        if (strategy == SqlServerValueGenerationStrategy.Sequence)
                        {
                            var sequence = modelBuilder.HasSequence(
                                property.GetSequenceName(declaringTable)
                                ?? entityType.GetRootType().ShortName() + modelBuilder.Metadata.GetSequenceNameSuffix(),
                                property.GetSequenceSchema(declaringTable)
                                ?? modelBuilder.Metadata.GetSequenceSchema()).Metadata;

                            property.Builder.HasDefaultValueSql(
                                RelationalDependencies.UpdateSqlGenerator.GenerateObtainNextSequenceValueOperation(
                                    sequence.Name, sequence.Schema));
                        }
                    */
                }
            }
        }

        bool IsStrategyNoneNeeded(IReadOnlyProperty property, StoreObjectIdentifier storeObject)
        {
            if (property.ValueGenerated == ValueGenerated.OnAdd
                && !property.TryGetDefaultValue(storeObject, out _)
                && property.GetDefaultValueSql(storeObject) == null
                && property.GetComputedColumnSql(storeObject) == null
                && property.DeclaringType.Model.GetValueGenerationStrategy() == VistaDBValueGenerationStrategy.IdentityColumn)
            {
                var providerClrType = (property.GetValueConverter()
                        ?? (property.FindRelationalTypeMapping(storeObject)
                            ?? Dependencies.TypeMappingSource.FindMapping((IProperty)property))?.Converter)
                    ?.ProviderClrType.UnwrapNullableType();

                return providerClrType != null
                    && (providerClrType.IsInteger() || providerClrType == typeof(decimal));
            }

            return false;
        }
    }
}
