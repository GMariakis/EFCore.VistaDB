// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.TestUtilities;

// Standalone stub: in the EF Core monorepo, [ConditionalFact] / [ConditionalTheory] skip tests
// when specific database providers or features are unavailable at runtime. In this standalone
// repo there are no such guards — the attributes behave identically to plain [Fact] / [Theory].
// Any provider-level availability guard is handled separately by [VistaDBInstalledFact].

[AttributeUsage(AttributeTargets.Method)]
public sealed class ConditionalFactAttribute : Xunit.FactAttribute { }

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ConditionalTheoryAttribute : Xunit.TheoryAttribute { }
