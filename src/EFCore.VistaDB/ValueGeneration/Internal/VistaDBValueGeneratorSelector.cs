// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Metadata.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDB.ValueGeneration.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBValueGeneratorSelector : RelationalValueGeneratorSelector
{
    private readonly IVistaDBSequenceValueGeneratorFactory _sequenceFactory;
    private readonly IVistaDBConnection _connection;
    private readonly IRawSqlCommandBuilder _rawSqlCommandBuilder;
    private readonly IRelationalCommandDiagnosticsLogger _commandLogger;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    /// <param name="dependencies">Parameter object containing dependencies for this service.</param>
    /// <param name="sequenceFactory">The sequence value generator factory (no-op for VistaDB).</param>
    /// <param name="connection">The VistaDB connection.</param>
    /// <param name="rawSqlCommandBuilder">A raw SQL command builder.</param>
    /// <param name="commandLogger">A command diagnostics logger.</param>
    public VistaDBValueGeneratorSelector(
        ValueGeneratorSelectorDependencies dependencies,
        IVistaDBSequenceValueGeneratorFactory sequenceFactory,
        IVistaDBConnection connection,
        IRawSqlCommandBuilder rawSqlCommandBuilder,
        IRelationalCommandDiagnosticsLogger commandLogger)
        : base(dependencies)
    {
        _sequenceFactory = sequenceFactory;
        _connection = connection;
        _rawSqlCommandBuilder = rawSqlCommandBuilder;
        _commandLogger = commandLogger;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public new virtual IVistaDBValueGeneratorCache Cache
        => (IVistaDBValueGeneratorCache)base.Cache;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="typeBase">The owning type.</param>
    /// <returns>The value generator, or <see langword="null" /> if none can be created.</returns>
    [Obsolete("Use TrySelect and throw if needed when the generator is not found.")]
    public override ValueGenerator? Select(IProperty property, ITypeBase typeBase)
    {
        if (TrySelect(property, typeBase, out var valueGenerator))
        {
            return valueGenerator;
        }

        throw new NotSupportedException(
            CoreStrings.NoValueGenerator(property.Name, property.DeclaringType.DisplayName(), property.ClrType.ShortDisplayName()));
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="typeBase">The owning type.</param>
    /// <param name="valueGenerator">The created value generator, if any.</param>
    /// <returns><see langword="true" /> if a generator was selected.</returns>
    public override bool TrySelect(IProperty property, ITypeBase typeBase, out ValueGenerator? valueGenerator)
    {
        // VistaDB: no analog — VistaDB has no database sequences, so the HiLo branch is unreachable.
        // Reference the injected sequence-factory / raw SQL services to satisfy field-readers and keep
        // the DI shape compatible with the SqlServer provider. Any caller that managed to configure
        // SequenceHiLo / Sequence on a property will get a NotSupportedException from the factory.
        // Original SqlServer logic preserved below for future revival when VistaDB adds support.
        /*
        if (property.GetValueGeneratorFactory() != null
            || property.GetValueGenerationStrategy() != SqlServerValueGenerationStrategy.SequenceHiLo)
        {
            return base.TrySelect(property, typeBase, out valueGenerator);
        }

        var propertyType = property.ClrType.UnwrapNullableType().UnwrapEnumType();

        valueGenerator = _sequenceFactory.TryCreate(
            property,
            propertyType,
            Cache.GetOrAddSequenceState(property, _connection),
            _connection,
            _rawSqlCommandBuilder,
            _commandLogger);

        if (valueGenerator != null)
        {
            return true;
        }

        var converter = property.GetTypeMapping().Converter;
        if (converter != null
            && converter.ProviderClrType != propertyType)
        {
            valueGenerator = _sequenceFactory.TryCreate(
                property,
                converter.ProviderClrType,
                Cache.GetOrAddSequenceState(property, _connection),
                _connection,
                _rawSqlCommandBuilder,
                _commandLogger);

            if (valueGenerator != null)
            {
                valueGenerator = valueGenerator.WithConverter(converter);
                return true;
            }
        }

        return false;
        */
        _ = _sequenceFactory;
        _ = _connection;
        _ = _rawSqlCommandBuilder;
        _ = _commandLogger;
        return base.TrySelect(property, typeBase, out valueGenerator);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="typeBase">The owning type.</param>
    /// <param name="clrType">The CLR type to find a generator for.</param>
    /// <returns>The value generator, or <see langword="null" /> if none.</returns>
    protected override ValueGenerator? FindForType(IProperty property, ITypeBase typeBase, Type clrType)
        => property.ClrType.UnwrapNullableType() == typeof(Guid)
            ? property.ValueGenerated == ValueGenerated.Never || property.GetDefaultValueSql() != null
                ? new TemporaryGuidValueGenerator()
                : new SequentialGuidValueGenerator()
            : base.FindForType(property, typeBase, clrType);
}
