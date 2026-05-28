// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Diagnostics.Internal;

namespace Microsoft.EntityFrameworkCore.TestUtilities;

public class VistaDBTestHelpers : RelationalTestHelpers
{
    protected VistaDBTestHelpers()
    {
    }

    public static VistaDBTestHelpers Instance { get; } = new();

    public override IServiceCollection AddProviderServices(IServiceCollection services)
        => services.AddEntityFrameworkVistaDB();

    public override DbContextOptionsBuilder UseProviderOptions(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseVistaDB("Data Source=DummyDatabase.vdb6");

    public override LoggingDefinitions LoggingDefinitions { get; } = new VistaDBLoggingDefinitions();
}
