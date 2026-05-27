// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Pure design-time resource-string sanity checks for every VistaDB <c>*NotSupported</c> string. The
///     parallel set under <c>EFCore.VistaDB.FunctionalTests</c> covers integration-level rejection sites;
///     this set lives in the unit-test assembly and exercises only the resource bundle.
/// </summary>
public class UnsupportedFeatureErrorMessagesTest
{
    [ConditionalFact]
    public void AtTimeZoneNotSupported_message_is_non_empty()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.AtTimeZoneNotSupported));

    [ConditionalFact]
    public void AzureSqlOptionsNotSupported_message_is_non_empty()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.AzureSqlOptionsNotSupported));

    [ConditionalFact]
    public void AzureSynapseOptionsNotSupported_message_is_non_empty()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.AzureSynapseOptionsNotSupported));

    [ConditionalFact]
    public void ComputedColumnsNotSupported_returns_format_with_tokens()
    {
        var s = VistaDBStrings.ComputedColumnsNotSupported("Name", "Pet");
        Assert.Contains("Name", s);
        Assert.Contains("Pet", s);
    }

    [ConditionalFact]
    public void CteNotSupported_message_is_non_empty()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.CteNotSupported));

    [ConditionalFact]
    public void DateDiffBigNotSupported_message_is_non_empty()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.DateDiffBigNotSupported));

    [ConditionalFact]
    public void DateFromPartsNotSupported_message_is_non_empty()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.DateFromPartsNotSupported));

    [ConditionalFact]
    public void ExtendedPropertiesNotSupported_message_is_non_empty()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.ExtendedPropertiesNotSupported));

    [ConditionalFact]
    public void FullTextSearchNotSupported_message_is_non_empty()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.FullTextSearchNotSupported));

    [ConditionalFact]
    public void MemoryOptimizedTablesNotSupported_message_is_non_empty()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.MemoryOptimizedTablesNotSupported));

    [ConditionalFact]
    public void MergeNotSupported_message_is_non_empty()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.MergeNotSupported));

    [ConditionalFact]
    public void OutputClauseNotSupported_message_is_non_empty()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.OutputClauseNotSupported));

    [ConditionalFact]
    public void SchemasNotSupported_returns_format_with_tokens()
    {
        var s = VistaDBStrings.SchemasNotSupported("Pet", "zoo");
        Assert.Contains("Pet", s);
        Assert.Contains("zoo", s);
    }

    [ConditionalFact]
    public void SequencesNotSupported_message_is_non_empty()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.SequencesNotSupported));

    [ConditionalFact]
    public void SqlVariantNotSupported_message_is_non_empty()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.SqlVariantNotSupported));

    [ConditionalFact]
    public void StoredProceduresNotSupported_message_is_non_empty()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.StoredProceduresNotSupported));

    [ConditionalFact]
    public void StructuralJsonNotSupported_message_is_non_empty()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.StructuralJsonNotSupported));

    [ConditionalFact]
    public void TemporalTablesNotSupported_returns_format_with_token()
    {
        var s = VistaDBStrings.TemporalTablesNotSupported("Pet");
        Assert.Contains("Pet", s);
    }

    [ConditionalFact]
    public void UdtNotSupported_message_is_non_empty()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.UdtNotSupported));

    [ConditionalFact]
    public void VectorTypeNotSupported_message_is_non_empty()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.VectorTypeNotSupported));

    [ConditionalFact]
    public void All_unsupported_strings_are_distinct()
    {
        var strings = new[]
        {
            VistaDBStrings.AtTimeZoneNotSupported,
            VistaDBStrings.CteNotSupported,
            VistaDBStrings.DateDiffBigNotSupported,
            VistaDBStrings.DateFromPartsNotSupported,
            VistaDBStrings.ExtendedPropertiesNotSupported,
            VistaDBStrings.FullTextSearchNotSupported,
            VistaDBStrings.MemoryOptimizedTablesNotSupported,
            VistaDBStrings.MergeNotSupported,
            VistaDBStrings.OutputClauseNotSupported,
            VistaDBStrings.SequencesNotSupported,
            VistaDBStrings.SqlVariantNotSupported,
            VistaDBStrings.StoredProceduresNotSupported,
            VistaDBStrings.StructuralJsonNotSupported,
            VistaDBStrings.UdtNotSupported,
            VistaDBStrings.VectorTypeNotSupported
        };

        Assert.Equal(strings.Length, strings.Distinct().Count());
    }
}
