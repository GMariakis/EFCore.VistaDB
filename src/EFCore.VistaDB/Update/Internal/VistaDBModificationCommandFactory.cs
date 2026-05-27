// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.VistaDB.Update.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBModificationCommandFactory : IModificationCommandFactory
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual IModificationCommand CreateModificationCommand(
        in ModificationCommandParameters modificationCommandParameters)
        => new ModificationCommand(modificationCommandParameters);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual INonTrackedModificationCommand CreateNonTrackedModificationCommand(
        in NonTrackedModificationCommandParameters modificationCommandParameters)
        => new ModificationCommand(modificationCommandParameters);

    // VistaDB: no analog — SqlServer subclasses ModificationCommand to override ProcessSinglePropertyJsonUpdate,
    // which rewrites column values as JSON_MODIFY(...) calls. VistaDB lacks JSON_MODIFY / JSON_VALUE / JSON_QUERY,
    // so we use the base ModificationCommand directly. Original SqlServer surface preserved below for future revival.
    /*
        public class SqlServerModificationCommand : ModificationCommand
        {
            public SqlServerModificationCommand(in ModificationCommandParameters parameters) : base(parameters) { }
            public SqlServerModificationCommand(in NonTrackedModificationCommandParameters parameters) : base(parameters) { }

            protected override void ProcessSinglePropertyJsonUpdate(ref ColumnModificationParameters parameters)
            {
                // ...JSON_MODIFY / JSON_VALUE / JSON_QUERY rewrite logic...
            }
        }
    */
}
