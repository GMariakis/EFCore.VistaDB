// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Internal;

namespace Microsoft.EntityFrameworkCore.VistaDBSpecific;

/// <summary>
///     Pure resource-string sanity checks for every VistaDB <c>*NotSupported</c> string. These assert
///     the messages are non-empty, contain expected tokens, and cover the full "VistaDB cannot do this"
///     surface area visible to consumers.
/// </summary>
public class UnsupportedFeatureErrorMessagesTest
{
    [ConditionalFact]
    public void AtTimeZoneNotSupported_message_is_present()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.AtTimeZoneNotSupported));

    [ConditionalFact]
    public void AzureSqlOptionsNotSupported_message_is_present()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.AzureSqlOptionsNotSupported));

    [ConditionalFact]
    public void AzureSynapseOptionsNotSupported_message_is_present()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.AzureSynapseOptionsNotSupported));

    [ConditionalFact]
    public void ComputedColumnsNotSupported_includes_property_and_entity_tokens()
    {
        var msg = VistaDBStrings.ComputedColumnsNotSupported("Name", "Pet");
        Assert.Contains("Name", msg);
        Assert.Contains("Pet", msg);
    }

    [ConditionalFact]
    public void CteNotSupported_message_is_present()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.CteNotSupported));

    [ConditionalFact]
    public void DateDiffBigNotSupported_message_is_present()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.DateDiffBigNotSupported));

    [ConditionalFact]
    public void DateFromPartsNotSupported_message_is_present()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.DateFromPartsNotSupported));

    [ConditionalFact]
    public void ExtendedPropertiesNotSupported_message_is_present()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.ExtendedPropertiesNotSupported));

    [ConditionalFact]
    public void FullTextSearchNotSupported_message_is_present()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.FullTextSearchNotSupported));

    [ConditionalFact]
    public void MemoryOptimizedTablesNotSupported_message_is_present()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.MemoryOptimizedTablesNotSupported));

    [ConditionalFact]
    public void MergeNotSupported_message_is_present()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.MergeNotSupported));

    [ConditionalFact]
    public void OutputClauseNotSupported_message_is_present()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.OutputClauseNotSupported));

    [ConditionalFact]
    public void SchemasNotSupported_includes_entity_and_schema_tokens()
    {
        var msg = VistaDBStrings.SchemasNotSupported("Pet", "zoo");
        Assert.Contains("Pet", msg);
        Assert.Contains("zoo", msg);
    }

    [ConditionalFact]
    public void SequencesNotSupported_message_is_present()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.SequencesNotSupported));

    [ConditionalFact]
    public void SqlVariantNotSupported_message_is_present()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.SqlVariantNotSupported));

    [ConditionalFact]
    public void StoredProceduresNotSupported_message_is_present()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.StoredProceduresNotSupported));

    [ConditionalFact]
    public void StructuralJsonNotSupported_message_is_present()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.StructuralJsonNotSupported));

    [ConditionalFact]
    public void TemporalTablesNotSupported_includes_entity_token()
    {
        var msg = VistaDBStrings.TemporalTablesNotSupported("Pet");
        Assert.Contains("Pet", msg);
    }

    [ConditionalFact]
    public void UdtNotSupported_message_is_present()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.UdtNotSupported));

    [ConditionalFact]
    public void VectorTypeNotSupported_message_is_present()
        => Assert.False(string.IsNullOrWhiteSpace(VistaDBStrings.VectorTypeNotSupported));

    [ConditionalFact]
    public void All_unsupported_strings_are_distinct()
    {
        // VistaDB: defensive — guard against accidental duplicate string IDs in the resx file.
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
