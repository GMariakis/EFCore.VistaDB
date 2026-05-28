// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Design.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

namespace Microsoft.EntityFrameworkCore.Design;

public class VistaDBDesignTimeProviderServicesTest : DesignTimeProviderServicesTest
{
    protected override Assembly GetRuntimeAssembly()
        => typeof(VistaDBConnection).Assembly;

    protected override Type GetDesignTimeServicesType()
        => typeof(VistaDBDesignTimeServices);
}
