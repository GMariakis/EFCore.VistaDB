// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Metadata.Internal;

// ReSharper disable InconsistentNaming
namespace Microsoft.EntityFrameworkCore.Metadata.Conventions;

public class VistaDBValueGenerationStrategyConventionTest
{
    [ConditionalFact]
    public void Annotations_are_added_when_conventional_model_builder_is_used()
    {
        var model = VistaDBTestHelpers.Instance.CreateConventionBuilder().Model;
        model.RemoveAnnotation(CoreAnnotationNames.ProductVersion);

        var annotations = model.GetAnnotations()
            .Where(a => a.Name.StartsWith(VistaDBAnnotationNames.Prefix, StringComparison.Ordinal)
                || a.Name == RelationalAnnotationNames.MaxIdentifierLength)
            .OrderBy(a => a.Name)
            .ToList();

        // VistaDB registers MaxIdentifierLength (128) plus the IdentityColumn value-generation strategy.
        Assert.Contains(annotations, a => a.Name == VistaDBAnnotationNames.ValueGenerationStrategy);
    }

    // VistaDB: no analog — VistaDB has no CREATE SEQUENCE, so HiLo and Sequence value-generation strategies are not supported.
    // Original SqlServer tests preserved below for future revival when VistaDB adds support.
    /*
    [ConditionalFact]
    public void Annotations_are_added_when_conventional_model_builder_is_used_with_sequences()
    {
        var model = SqlServerTestHelpers.Instance.CreateConventionBuilder()
            .UseHiLo()
            .Model;

        model.RemoveAnnotation(CoreAnnotationNames.ProductVersion);

        var annotations = model.GetAnnotations().OrderBy(a => a.Name).ToList();
        Assert.Equal(4, annotations.Count);

        Assert.Equal(RelationalAnnotationNames.MaxIdentifierLength, annotations[0].Name);

        Assert.Equal(
            RelationalAnnotationNames.Sequences,
            annotations[1].Name);
        Assert.NotNull(annotations[1].Value);

        Assert.Equal(SqlServerAnnotationNames.HiLoSequenceName, annotations[2].Name);
        Assert.Equal(SqlServerModelExtensions.DefaultHiLoSequenceName, annotations[2].Value);

        Assert.Equal(SqlServerAnnotationNames.ValueGenerationStrategy, annotations[3].Name);
        Assert.Equal(SqlServerValueGenerationStrategy.SequenceHiLo, annotations[3].Value);
    }

    [ConditionalFact]
    public void Annotations_are_added_when_conventional_model_builder_is_used_with_key_sequences()
    {
        var model = SqlServerTestHelpers.Instance.CreateConventionBuilder()
            .UseKeySequences()
            .Model;

        model.RemoveAnnotation(CoreAnnotationNames.ProductVersion);

        var annotations = model.GetAnnotations().OrderBy(a => a.Name).ToList();
        Assert.Equal(3, annotations.Count);

        Assert.Equal(RelationalAnnotationNames.MaxIdentifierLength, annotations[0].Name);

        Assert.Equal(SqlServerAnnotationNames.SequenceNameSuffix, annotations[1].Name);
        Assert.Equal(SqlServerModelExtensions.DefaultSequenceNameSuffix, annotations[1].Value);

        Assert.Equal(SqlServerAnnotationNames.ValueGenerationStrategy, annotations[2].Name);
        Assert.Equal(SqlServerValueGenerationStrategy.Sequence, annotations[2].Value);
    }
    */
}
