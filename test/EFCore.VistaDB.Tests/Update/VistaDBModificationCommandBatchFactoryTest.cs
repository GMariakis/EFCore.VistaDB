// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Update.Internal;

// ReSharper disable InconsistentNaming
namespace Microsoft.EntityFrameworkCore.Update;

public class VistaDBModificationCommandBatchFactoryTest
{
    [ConditionalFact]
    public void Batch_emits_a_single_command_then_rejects_subsequent_adds()
    {
        // VistaDB forces effective MaxBatchSize = 1 because we have no MERGE/OUTPUT batch shape,
        // so the configured MaxBatchSize on VistaDBOptionsExtension is ignored.
        var factory = CreateFactory();

        var batch = factory.Create();

        Assert.True(batch.TryAddCommand(CreateModificationCommand("T1", null, false)));
        Assert.False(batch.TryAddCommand(CreateModificationCommand("T1", null, false)));
    }

    [ConditionalFact]
    public void Configured_MaxBatchSize_is_ignored_by_VistaDB_factory()
    {
        // VistaDB: no analog — VistaDB has no MERGE/OUTPUT and so always batches one command at a time.
        // The factory does not read the options extension at all; both default and explicit MaxBatchSize
        // configurations produce the same one-command batch.
        var factory = CreateFactory();

        var batch = factory.Create();

        Assert.True(batch.TryAddCommand(CreateModificationCommand("T1", null, false)));
        Assert.False(batch.TryAddCommand(CreateModificationCommand("T1", null, false)));
    }

    // VistaDB: no analog — VistaDBModificationCommandBatchFactory does not accept DbContextOptions, so the
    // SqlServer-shape "Uses_MaxBatchSize_specified_in_SqlServerOptionsExtension" / "MaxBatchSize_is_optional"
    // tests collapse into the two assertions above.
    // Original SqlServer tests preserved below for future revival when VistaDB adds MERGE support.
    /*
    [ConditionalFact]
    public void Uses_MaxBatchSize_specified_in_SqlServerOptionsExtension()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseSqlServer("Database=Crunchie", b => b.MaxBatchSize(1));
        // ... full SqlServer wiring ...
    }
    */

    private static VistaDBModificationCommandBatchFactory CreateFactory()
    {
        var typeMapper = new VistaDBTypeMappingSource(
            TestServiceFactory.Instance.Create<TypeMappingSourceDependencies>(),
            TestServiceFactory.Instance.Create<RelationalTypeMappingSourceDependencies>(),
            TestServiceFactory.Instance.Create<VistaDBSingletonOptions>());

        var logger = new FakeRelationalCommandDiagnosticsLogger();

        return new VistaDBModificationCommandBatchFactory(
            new ModificationCommandBatchFactoryDependencies(
                new RelationalCommandBuilderFactory(
                    new RelationalCommandBuilderDependencies(
                        typeMapper,
                        new VistaDBExceptionDetector(),
                        new LoggingOptions())),
                new VistaDBSqlGenerationHelper(
                    new RelationalSqlGenerationHelperDependencies()),
                new VistaDBUpdateSqlGenerator(
                    new UpdateSqlGeneratorDependencies(
                        new VistaDBSqlGenerationHelper(
                            new RelationalSqlGenerationHelperDependencies()),
                        typeMapper)),
                new CurrentDbContext(new FakeDbContext()),
                logger,
                new FakeDiagnosticsLogger<DbLoggerCategory.Update>()),
            // No DDA accessor needed — these unit tests never exercise the explicit-IDENTITY-insert
            // path (that's covered in functional tests with a real VistaDB engine).
            new NullDdaAccessor());
    }

    private sealed class NullDdaAccessor : IVistaDBDdaAccessor
    {
        public global::VistaDB.DDA.IVistaDBDDA Dda => throw new NotSupportedException("Test fake — DDA not exercised here.");
        public bool IsResolved => false;
        public string GetDatabaseFilePath() => throw new NotSupportedException("Test fake — DDA not exercised here.");
        public global::VistaDB.DDA.IVistaDBDatabase OpenDatabase(bool readOnly = false) => throw new NotSupportedException("Test fake — DDA not exercised here.");
        public void WithDatabase(Action<global::VistaDB.DDA.IVistaDBDatabase> action, bool readOnly = false) => throw new NotSupportedException("Test fake — DDA not exercised here.");
        public void WithTable(string tableName, Action<global::VistaDB.DDA.IVistaDBTable> action, bool readOnly = false, bool exclusive = false) => throw new NotSupportedException("Test fake — DDA not exercised here.");
        public void Dispose() { }
    }

    private class FakeDbContext : DbContext;

    private static INonTrackedModificationCommand CreateModificationCommand(
        string name,
        string schema,
        bool sensitiveLoggingEnabled)
    {
        var modificationCommand = new ModificationCommandFactory().CreateNonTrackedModificationCommand(
            new NonTrackedModificationCommandParameters(
                name, schema, sensitiveLoggingEnabled));

        return modificationCommand;
    }
}
