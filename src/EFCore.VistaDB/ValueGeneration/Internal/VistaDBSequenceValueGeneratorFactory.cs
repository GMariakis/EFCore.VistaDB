// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDB.ValueGeneration.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBSequenceValueGeneratorFactory : IVistaDBSequenceValueGeneratorFactory
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBSequenceValueGeneratorFactory()
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="type">The CLR type of the property.</param>
    /// <param name="connection">The VistaDB connection.</param>
    /// <param name="rawSqlCommandBuilder">A raw SQL command builder.</param>
    /// <param name="commandLogger">A command diagnostics logger.</param>
    /// <returns>This method always throws <see cref="NotSupportedException" />.</returns>
    /// <exception cref="NotSupportedException">Always thrown — VistaDB does not support sequences.</exception>
    public virtual ValueGenerator? TryCreate(
        IProperty property,
        Type type,
        IVistaDBConnection connection,
        IRawSqlCommandBuilder rawSqlCommandBuilder,
        IRelationalCommandDiagnosticsLogger commandLogger)
    {
        // VistaDB: no analog — VistaDB has no database sequences, so a HiLo-from-sequence generator
        // cannot be created. Any call here means a model was configured with UseHiLo / UseSequence,
        // which should have been blocked earlier; throw to make the misuse loud.
        // Original SqlServer logic preserved below for future revival when VistaDB adds support.
        /*
        if (type == typeof(long))
        {
            return new SqlServerSequenceHiLoValueGenerator<long>(
                rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
        }

        if (type == typeof(int))
        {
            return new SqlServerSequenceHiLoValueGenerator<int>(
                rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
        }

        if (type == typeof(decimal))
        {
            return new SqlServerSequenceHiLoValueGenerator<decimal>(
                rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
        }

        if (type == typeof(short))
        {
            return new SqlServerSequenceHiLoValueGenerator<short>(
                rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
        }

        if (type == typeof(byte))
        {
            return new SqlServerSequenceHiLoValueGenerator<byte>(
                rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
        }

        if (type == typeof(char))
        {
            return new SqlServerSequenceHiLoValueGenerator<char>(
                rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
        }

        if (type == typeof(ulong))
        {
            return new SqlServerSequenceHiLoValueGenerator<ulong>(
                rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
        }

        if (type == typeof(uint))
        {
            return new SqlServerSequenceHiLoValueGenerator<uint>(
                rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
        }

        if (type == typeof(ushort))
        {
            return new SqlServerSequenceHiLoValueGenerator<ushort>(
                rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
        }

        if (type == typeof(sbyte))
        {
            return new SqlServerSequenceHiLoValueGenerator<sbyte>(
                rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
        }

        return null;
        */
        throw new NotSupportedException(VistaDBStrings.SequencesNotSupported);
    }
}
