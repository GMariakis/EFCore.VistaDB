// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// VistaDB: no analog — VistaDB has no CREATE SEQUENCE. This file is excluded from compilation in the
// csproj. The class declaration is preserved (commented) for discoverability so a reviver can drop the
// Compile Remove entry and uncomment the body when VistaDB gains sequence support.
// Original SqlServer logic preserved below for future revival.
/*
namespace Microsoft.EntityFrameworkCore.VistaDB.ValueGeneration.Internal;

public class VistaDBSequenceValueGeneratorState : HiLoValueGeneratorState
{
    public VistaDBSequenceValueGeneratorState(ISequence sequence)
        : base(sequence.IncrementBy)
        => Sequence = sequence;

    public virtual ISequence Sequence { get; }
}
*/
