// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Infrastructure;
using Microsoft.EntityFrameworkCore.VistaDB.Scaffolding.Internal;

// ReSharper disable InconsistentNaming
namespace Microsoft.EntityFrameworkCore.Scaffolding;

public class VistaDBCodeGeneratorTest
{
    [ConditionalFact]
    public virtual void Use_provider_method_is_generated_correctly()
    {
        var codeGenerator = new VistaDBCodeGenerator(
            new ProviderCodeGeneratorDependencies(
                []));

        var result = codeGenerator.GenerateUseProvider("Data Source=Test.vdb6", providerOptions: null);

        Assert.Equal("UseVistaDB", result.Method);
        Assert.Collection(
            result.Arguments,
            a => Assert.Equal("Data Source=Test.vdb6", a));
        Assert.Null(result.ChainedCall);
    }

    [ConditionalFact]
    public virtual void Use_provider_method_is_generated_correctly_with_options()
    {
        var codeGenerator = new VistaDBCodeGenerator(
            new ProviderCodeGeneratorDependencies(
                []));

        var providerOptions = new MethodCallCodeFragment(_setProviderOptionMethodInfo);

        var result = codeGenerator.GenerateUseProvider("Data Source=Test.vdb6", providerOptions);

        Assert.Equal("UseVistaDB", result.Method);
        Assert.Collection(
            result.Arguments,
            a => Assert.Equal("Data Source=Test.vdb6", a),
            a =>
            {
                var nestedClosure = Assert.IsType<NestedClosureCodeFragment>(a);

                Assert.Equal("x", nestedClosure.Parameter);
                Assert.Same(providerOptions, nestedClosure.MethodCalls[0]);
            });
        Assert.Null(result.ChainedCall);
    }

    // VistaDB: no analog — VistaDB has no NetTopologySuite (spatial) plugin.
    // Original SqlServer test preserved below for future revival when VistaDB adds support.
    /*
    [ConditionalFact]
    public virtual void Use_provider_method_is_generated_correctly_with_NetTopologySuite()
    {
        var codeGenerator = new SqlServerCodeGenerator(
            new ProviderCodeGeneratorDependencies(
                [new SqlServerNetTopologySuiteCodeGeneratorPlugin()]));

        var result = ((IProviderConfigurationCodeGenerator)codeGenerator).GenerateUseProvider("Data Source=Test");

        Assert.Equal("UseSqlServer", result.Method);
        // ...
    }
    */

    private static readonly MethodInfo _setProviderOptionMethodInfo
        = typeof(VistaDBCodeGeneratorTest).GetRuntimeMethod(nameof(SetProviderOption), [typeof(DbContextOptionsBuilder)]);

    public static VistaDBDbContextOptionsBuilder SetProviderOption(DbContextOptionsBuilder optionsBuilder)
        => throw new NotSupportedException();
}
