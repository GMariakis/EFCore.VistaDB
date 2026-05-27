// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.EntityFrameworkCore.VistaDB.Infrastructure.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBOptionsExtension : RelationalOptionsExtension, IDbContextOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;

    // VistaDB: no analog — VistaDB has a single engine (file-based .vdb6); no engine type or
    // compatibility-level state is tracked here.
    // Original SqlServer fields preserved below for future revival if VistaDB introduces variants.
    /*
        private SqlServerEngineType _engineType;
        private int? _sqlServerCompatibilityLevel;
        private int? _azureSqlCompatibilityLevel;
        private int? _azureSynapseCompatibilityLevel;
        private bool _useRetryingStrategyByDefault;
    */

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBOptionsExtension()
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected VistaDBOptionsExtension(VistaDBOptionsExtension copyFrom)
        : base(copyFrom)
    {
    }

    /// <inheritdoc />
    public override DbContextOptionsExtensionInfo Info
        => _info ??= new ExtensionInfo(this);

    /// <inheritdoc />
    protected override RelationalOptionsExtension Clone()
        => new VistaDBOptionsExtension(this);

    /// <inheritdoc />
    public override void ApplyServices(IServiceCollection services)
        => services.AddEntityFrameworkVistaDB();

    private sealed class ExtensionInfo(IDbContextOptionsExtension extension) : RelationalExtensionInfo(extension)
    {
        private string? _logFragment;

        private new VistaDBOptionsExtension Extension
            => (VistaDBOptionsExtension)base.Extension;

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => other is ExtensionInfo;

        public override string LogFragment
        {
            get
            {
                if (_logFragment == null)
                {
                    var builder = new StringBuilder();
                    builder.Append(base.LogFragment);
                    _logFragment = builder.ToString();
                }

                return _logFragment;
            }
        }

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            debugInfo["VistaDB"] = "1";
        }
    }
}
