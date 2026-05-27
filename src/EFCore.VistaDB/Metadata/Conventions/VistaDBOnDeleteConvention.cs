// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Metadata.Conventions;

/// <summary>
///     Extends <see cref="CascadeDeleteConvention" /> to convert <see cref="DeleteBehavior.Cascade" />
///     to <see cref="DeleteBehavior.ClientCascade" /> for self-referencing skip navigations that share
///     a join table — the same pattern SQL Server enforces to avoid "multiple cascade paths" errors.
///     VistaDB does not throw on such configurations, but the spec tests expect the same model shape
///     as SQL Server (and having consistent semantics with SQL Server reduces migration surprises when
///     targeting both providers).
///
///     This is a direct port of <c>SqlServerOnDeleteConvention</c> with no functional changes.
/// </summary>
/// <remarks>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </remarks>
public class VistaDBOnDeleteConvention : CascadeDeleteConvention,
    ISkipNavigationForeignKeyChangedConvention,
    IEntityTypeAnnotationChangedConvention
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBOnDeleteConvention(
        ProviderConventionSetBuilderDependencies dependencies,
        RelationalConventionSetBuilderDependencies relationalDependencies)
        : base(dependencies)
        => RelationalDependencies = relationalDependencies;

    /// <summary>Relational provider-specific dependencies for this service.</summary>
    protected virtual RelationalConventionSetBuilderDependencies RelationalDependencies { get; }

    /// <inheritdoc />
    public virtual void ProcessSkipNavigationForeignKeyChanged(
        IConventionSkipNavigationBuilder skipNavigationBuilder,
        IConventionForeignKey? foreignKey,
        IConventionForeignKey? oldForeignKey,
        IConventionContext<IConventionForeignKey> context)
    {
        if (foreignKey is not null && foreignKey.IsInModel)
        {
            foreignKey.Builder.OnDelete(GetTargetDeleteBehavior(foreignKey));
        }
    }

    /// <inheritdoc />
    protected override DeleteBehavior GetTargetDeleteBehavior(IConventionForeignKey foreignKey)
    {
        var deleteBehavior = base.GetTargetDeleteBehavior(foreignKey);
        if (deleteBehavior != DeleteBehavior.Cascade)
        {
            return deleteBehavior;
        }

        return ProcessSkipNavigations(foreignKey.GetReferencingSkipNavigations()) ?? deleteBehavior;
    }

    private DeleteBehavior? ProcessSkipNavigations(IEnumerable<IConventionSkipNavigation> skipNavigations)
    {
        var skipNavigation = skipNavigations
            .FirstOrDefault(s => s.Inverse != null
                && IsMappedToSameTable(s.DeclaringEntityType, s.TargetEntityType));

        if (skipNavigation != null && skipNavigation.ForeignKey != null)
        {
            var isFirstSkipNavigation = IsFirstSkipNavigation(skipNavigation);
            if (!isFirstSkipNavigation)
            {
                skipNavigation = skipNavigation.Inverse!;
            }

            var inverseSkipNavigation = skipNavigation.Inverse!;

            var deleteBehavior = DefaultDeleteBehavior(skipNavigation);
            var inverseDeleteBehavior = DefaultDeleteBehavior(inverseSkipNavigation);

            if (deleteBehavior == DeleteBehavior.Cascade
                && inverseDeleteBehavior == DeleteBehavior.Cascade
                && !(inverseSkipNavigation.ForeignKey!.GetDeleteBehaviorConfigurationSource() == ConfigurationSource.Explicit
                    && inverseSkipNavigation.ForeignKey!.DeleteBehavior != DeleteBehavior.Cascade))
            {
                deleteBehavior = DeleteBehavior.ClientCascade;
            }

            skipNavigation.ForeignKey!.Builder.OnDelete(deleteBehavior);
            inverseSkipNavigation.ForeignKey!.Builder.OnDelete(inverseDeleteBehavior);

            return isFirstSkipNavigation ? deleteBehavior : inverseDeleteBehavior;
        }

        return null;

        DeleteBehavior DefaultDeleteBehavior(IConventionSkipNavigation nav)
            => nav.ForeignKey!.IsRequired ? DeleteBehavior.Cascade : DeleteBehavior.ClientSetNull;

        bool IsMappedToSameTable(IConventionEntityType entityType1, IConventionEntityType entityType2)
        {
            var tableName1 = entityType1.GetTableName();
            var tableName2 = entityType2.GetTableName();
            return tableName1 != null
                && tableName2 != null
                && tableName1 == tableName2
                && entityType1.GetSchema() == entityType2.GetSchema();
        }

        bool IsFirstSkipNavigation(IConventionSkipNavigation navigation)
            => navigation.DeclaringEntityType != navigation.TargetEntityType
                ? string.Compare(navigation.DeclaringEntityType.Name, navigation.TargetEntityType.Name,
                    StringComparison.Ordinal) < 0
                : string.Compare(navigation.Name, navigation.Inverse!.Name, StringComparison.Ordinal) < 0;
    }

    /// <inheritdoc />
    public virtual void ProcessEntityTypeAnnotationChanged(
        IConventionEntityTypeBuilder entityTypeBuilder,
        string name,
        IConventionAnnotation? annotation,
        IConventionAnnotation? oldAnnotation,
        IConventionContext<IConventionAnnotation> context)
    {
        if (name is RelationalAnnotationNames.TableName or RelationalAnnotationNames.Schema)
        {
            ProcessSkipNavigations(entityTypeBuilder.Metadata.GetDeclaredSkipNavigations());

            foreach (var foreignKey in entityTypeBuilder.Metadata.GetDeclaredForeignKeys())
            {
                var deleteBehavior = GetTargetDeleteBehavior(foreignKey);
                if (foreignKey.DeleteBehavior != deleteBehavior)
                {
                    foreignKey.Builder.OnDelete(deleteBehavior);
                }
            }
        }
    }
}
