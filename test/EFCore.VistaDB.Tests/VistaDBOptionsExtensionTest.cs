// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.VistaDB.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

namespace Microsoft.EntityFrameworkCore;

public class VistaDBOptionsExtensionTest
{
    [ConditionalFact]
    public void UseVistaDB_registers_VistaDBOptionsExtension_with_connection_string()
    {
        var optionsBuilder = new DbContextOptionsBuilder()
            .UseVistaDB("Data Source=test.vdb6");

        var extension = optionsBuilder.Options.FindExtension<VistaDBOptionsExtension>();
        Assert.NotNull(extension);
        Assert.Contains("Data Source=test.vdb6", extension.ConnectionString);
    }

    [ConditionalFact]
    public void AddEntityFrameworkVistaDB_registers_required_VistaDB_services()
    {
        var services = new ServiceCollection().AddEntityFrameworkVistaDB();

        Assert.Contains(services, sd => sd.ServiceType == typeof(IVistaDBConnection));
        Assert.Contains(services, sd => sd.ServiceType == typeof(IVistaDBSingletonOptions));
    }

    [ConditionalFact]
    public void DbContext_resolved_through_DI_has_VistaDB_connection()
    {
        var services = new ServiceCollection()
            .AddDbContext<EmptyDIContext>(o => o.UseVistaDB("Data Source=test.vdb6"))
            .BuildServiceProvider();

        using var scope = services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<EmptyDIContext>();
        Assert.NotNull(ctx);

        var connection = ctx.Database.GetDbConnection();
        Assert.IsType<global::VistaDB.Provider.VistaDBConnection>(connection);
    }

    [ConditionalFact]
    public void ApplyServices_adds_correct_services()
    {
        var services = new ServiceCollection();

        new VistaDBOptionsExtension().ApplyServices(services);

        Assert.Contains(services, sd => sd.ServiceType == typeof(IVistaDBConnection));
        Assert.Contains(services, sd => sd.ServiceType == typeof(IVistaDBSingletonOptions));
    }

    [ConditionalFact]
    public void Compiled_model_is_thread_safe()
    {
        var tasks = new Task[Environment.ProcessorCount];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                using var ctx = new EmptyContext();
                Assert.NotNull(ctx.Model.GetRelationalDependencies());
            });
        }

        Task.WaitAll(tasks);
    }

    private class EmptyDIContext : DbContext
    {
        public EmptyDIContext(DbContextOptions<EmptyDIContext> options)
            : base(options)
        {
        }
    }

    private class EmptyContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseVistaDB("Data Source=test.vdb6").UseModel(EmptyContextModel.Instance);
            }
        }
    }

    [DbContext(typeof(EmptyContext))]
    private class EmptyContextModel(bool skipDetectChanges, Guid modelId, int entityTypeCount, int typeConfigurationCount)
        : Microsoft.EntityFrameworkCore.Metadata.RuntimeModel(
            skipDetectChanges, modelId, entityTypeCount, typeConfigurationCount)
    {
        static EmptyContextModel()
        {
            var model = new EmptyContextModel(false, Guid.NewGuid(), 0, 0);
            _instance = model;
        }

        private static readonly EmptyContextModel _instance;

        public static IModel Instance
            => _instance;
    }
}
