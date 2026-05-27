// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestUtilities.FakeProvider;
using Microsoft.EntityFrameworkCore.VistaDB.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

namespace Microsoft.EntityFrameworkCore;

public class VistaDBDatabaseFacadeExtensionsTest
{
    [ConditionalFact]
    public void Returns_appropriate_name()
        => Assert.Equal(
            typeof(VistaDBConnection).Assembly.GetName().Name,
            new DatabaseProvider<VistaDBOptionsExtension>(new DatabaseProviderDependencies()).Name);

    [ConditionalFact]
    public void Is_configured_when_configuration_contains_associated_extension()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseVistaDB("Data Source=Crunchie.vdb6");

        Assert.True(
            new DatabaseProvider<VistaDBOptionsExtension>(new DatabaseProviderDependencies()).IsConfigured(optionsBuilder.Options));
    }

    [ConditionalFact]
    public void Is_not_configured_when_configuration_does_not_contain_associated_extension()
    {
        var optionsBuilder = new DbContextOptionsBuilder();

        Assert.False(
            new DatabaseProvider<VistaDBOptionsExtension>(new DatabaseProviderDependencies()).IsConfigured(optionsBuilder.Options));
    }

    [ConditionalFact]
    public void Default_value_for_CommandTimeout_is_null_and_can_be_changed_including_setting_to_null()
    {
        using var context = new TimeoutContext();
        Assert.Null(context.Database.GetCommandTimeout());

        context.Database.SetCommandTimeout(77);
        Assert.Equal(77, context.Database.GetCommandTimeout());

        context.Database.SetCommandTimeout(null);
        Assert.Null(context.Database.GetCommandTimeout());

        context.Database.SetCommandTimeout(TimeSpan.FromSeconds(66));
        Assert.Equal(66, context.Database.GetCommandTimeout());
    }

    [ConditionalFact]
    public void Setting_CommandTimeout_to_infinite_sets_to_zero()
    {
        using var context = new TimeoutContext();

        context.Database.SetCommandTimeout(Timeout.InfiniteTimeSpan);
        Assert.Equal(0, context.Database.GetCommandTimeout());
    }

    [ConditionalFact]
    public void Setting_CommandTimeout_to_negative_value_throws()
    {
        Assert.Throws<InvalidOperationException>(() => new DbContextOptionsBuilder().UseVistaDB(
            "No=LoveyDovey",
            b => b.CommandTimeout(-55)));

        using var context = new TimeoutContext();
        Assert.Null(context.Database.GetCommandTimeout());

        Assert.Throws<ArgumentException>(() => context.Database.SetCommandTimeout(-3));
        Assert.Throws<ArgumentException>(() => context.Database.SetCommandTimeout(TimeSpan.FromSeconds(-3)));

        Assert.Throws<ArgumentException>(() => context.Database.SetCommandTimeout(-99));
        Assert.Throws<ArgumentException>(() => context.Database.SetCommandTimeout(TimeSpan.FromSeconds(-99)));

        Assert.Throws<ArgumentException>(() => context.Database.SetCommandTimeout(TimeSpan.FromSeconds(uint.MaxValue)));
    }

    public class TimeoutContext : DbContext
    {
        public TimeoutContext()
        {
        }

        public TimeoutContext(int? commandTimeout)
            => Database.SetCommandTimeout(commandTimeout);

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseInternalServiceProvider(VistaDBFixture.DefaultServiceProvider)
                .UseVistaDB(new FakeDbConnection("A=B"));
    }

    // VistaDB: no analog — VistaDB does not yet expose a Database.IsVistaDB() helper.
    // Original SqlServer test preserved below for future revival when VistaDB adds support.
    /*
    [ConditionalFact]
    public void IsSqlServer_when_using_OnConfiguring()
    {
        using var context = new SqlServerOnConfiguringContext();
        Assert.True(context.Database.IsSqlServer());
    }

    [ConditionalFact]
    public void IsSqlServer_in_OnModelCreating_when_using_OnConfiguring()
    {
        using var context = new SqlServerOnModelContext();
        var _ = context.Model;
        Assert.True(context.IsSqlServerSet);
    }
    // ... etc.
    */

    [ConditionalFact]
    public void IsRelational_in_OnModelCreating_when_using_OnConfiguring()
    {
        using var context = new RelationalOnModelContext();
        var _ = context.Model;
        Assert.True(context.IsRelationalSet);
    }

    private class ProviderContext : DbContext
    {
        protected ProviderContext()
        {
        }

        public ProviderContext(DbContextOptions options)
            : base(options)
        {
        }

        public bool? IsRelationalSet { get; protected set; }
    }

    private class VistaDBOnConfiguringContext : ProviderContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseInternalServiceProvider(VistaDBFixture.DefaultServiceProvider)
                .UseVistaDB("Data Source=Maltesers.vdb6");
    }

    private class RelationalOnModelContext : VistaDBOnConfiguringContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => IsRelationalSet = Database.IsRelational();
    }
}
