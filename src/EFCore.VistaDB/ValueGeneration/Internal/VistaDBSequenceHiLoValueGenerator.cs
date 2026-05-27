// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// VistaDB: no analog — VistaDB has no CREATE SEQUENCE, so there is no NEXT VALUE FOR sequence and the
// HiLo pattern cannot be backed by a database sequence. This file is excluded from compilation in the
// csproj; the class declaration is preserved (commented) so a reviver can drop the Compile Remove entry
// and uncomment when VistaDB gains sequence support.
// Original SqlServer logic preserved below for future revival.
/*
using System.Globalization;
using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDB.ValueGeneration.Internal;

public class VistaDBSequenceHiLoValueGenerator<TValue> : HiLoValueGenerator<TValue>
{
    private readonly IRawSqlCommandBuilder _rawSqlCommandBuilder;
    private readonly IUpdateSqlGenerator _sqlGenerator;
    private readonly IVistaDBConnection _connection;
    private readonly ISequence _sequence;
    private readonly IRelationalCommandDiagnosticsLogger _commandLogger;

    public VistaDBSequenceHiLoValueGenerator(
        IRawSqlCommandBuilder rawSqlCommandBuilder,
        IUpdateSqlGenerator sqlGenerator,
        VistaDBSequenceValueGeneratorState generatorState,
        IVistaDBConnection connection,
        IRelationalCommandDiagnosticsLogger commandLogger)
        : base(generatorState)
    {
        _sequence = generatorState.Sequence;
        _rawSqlCommandBuilder = rawSqlCommandBuilder;
        _sqlGenerator = sqlGenerator;
        _connection = connection;
        _commandLogger = commandLogger;
    }

    protected override long GetNewLowValue()
        => (long)Convert.ChangeType(
            _rawSqlCommandBuilder
                .Build(_sqlGenerator.GenerateNextSequenceValueOperation(_sequence.Name, _sequence.Schema))
                .ExecuteScalar(
                    new RelationalCommandParameterObject(
                        _connection,
                        parameterValues: null,
                        readerColumns: null,
                        context: null,
                        _commandLogger, CommandSource.ValueGenerator)),
            typeof(long),
            CultureInfo.InvariantCulture)!;

    protected override async Task<long> GetNewLowValueAsync(CancellationToken cancellationToken = default)
        => (long)Convert.ChangeType(
            await _rawSqlCommandBuilder
                .Build(_sqlGenerator.GenerateNextSequenceValueOperation(_sequence.Name, _sequence.Schema))
                .ExecuteScalarAsync(
                    new RelationalCommandParameterObject(
                        _connection,
                        parameterValues: null,
                        readerColumns: null,
                        context: null,
                        _commandLogger, CommandSource.ValueGenerator),
                    cancellationToken)
                .ConfigureAwait(false),
            typeof(long),
            CultureInfo.InvariantCulture)!;

    public override bool GeneratesTemporaryValues
        => false;
}
*/
