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

public class VistaDBModificationCommandBatchTest
{
    [ConditionalTheory, InlineData(EntityState.Added), InlineData(EntityState.Deleted), InlineData(EntityState.Modified)]
    public void AddCommand_returns_false_after_first_command_is_accepted(EntityState entityState)
    {
        // VistaDB forces effective MaxBatchSize = 1 because it has no MERGE/OUTPUT batch shape. Unlike
        // SqlServer which parametrizes maxBatchSize, the VistaDB batch is always single-command.
        var batch = CreateBatch();

        var firstCommand = CreateModificationCommand("T1", null, false);
        firstCommand.EntityState = entityState;
        var secondCommand = CreateModificationCommand("T1", null, false);
        secondCommand.EntityState = entityState;

        Assert.True(batch.TryAddCommand(firstCommand));
        Assert.False(batch.TryAddCommand(secondCommand));

        Assert.Same(firstCommand, Assert.Single(batch.ModificationCommands));
    }

    [ConditionalFact]
    public void Batch_emits_per_row_INSERT_with_SCOPE_IDENTITY_select_shape()
    {
        // VistaDB: no analog — SqlServer accumulates pending INSERTs into MERGE ... OUTPUT. VistaDB
        // emits one INSERT per row followed by SELECT SCOPE_IDENTITY() when an identity column is bound
        // to the read-back. This test asserts the absence of MERGE in the produced SQL when the batch
        // completes — finer-grained SQL assertions live in the FunctionalTests project against a real engine.
        var batch = CreateBatch();
        var command = CreateModificationCommand("T1", null, false);
        command.EntityState = EntityState.Added;

        Assert.True(batch.TryAddCommand(command));
        batch.Complete(moreBatchesExpected: false);

        Assert.DoesNotContain("MERGE", batch.StoreCommand.RelationalCommand.CommandText);
        Assert.DoesNotContain("OUTPUT", batch.StoreCommand.RelationalCommand.CommandText);
    }

    // VistaDB: no analog — VistaDB has no 2100-parameter limit and no MERGE-coalescing. The SqlServer
    // parameter-count and pending-bulk-insert tests don't apply.
    // Original SqlServer tests preserved below for future revival when VistaDB adds support.
    /*
    [ConditionalTheory, InlineData(EntityState.Added, true) ...]
    public void AddCommand_returns_false_when_max_parameters_are_reached(EntityState entityState, bool withSameTable) { ... }

    [ConditionalTheory, InlineData(true), InlineData(false)]
    public void AddCommand_when_max_parameters_are_reached_with_pending_commands(bool lastCommandPending) { ... }
    */

    private class FakeDbContext : DbContext;

    private static TestVistaDBModificationCommandBatch CreateBatch()
    {
        var typeMapper = CreateTypeMappingSource();

        return new TestVistaDBModificationCommandBatch(
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
                new FakeRelationalCommandDiagnosticsLogger(),
                new FakeDiagnosticsLogger<DbLoggerCategory.Update>()));
    }

    private static VistaDBTypeMappingSource CreateTypeMappingSource()
        => new(
            TestServiceFactory.Instance.Create<TypeMappingSourceDependencies>(),
            TestServiceFactory.Instance.Create<RelationalTypeMappingSourceDependencies>(),
            TestServiceFactory.Instance.Create<VistaDBSingletonOptions>());

    private static INonTrackedModificationCommand CreateModificationCommand(
        string name,
        string schema,
        bool sensitiveLoggingEnabled)
        => new ModificationCommandFactory().CreateNonTrackedModificationCommand(
            new NonTrackedModificationCommandParameters(name, schema, sensitiveLoggingEnabled));

    private class TestVistaDBModificationCommandBatch(ModificationCommandBatchFactoryDependencies dependencies)
        : VistaDBModificationCommandBatch(dependencies, new NullDdaAccessor())
    {
        public new Dictionary<string, object> ParameterValues
            => base.ParameterValues;

        public new RawSqlCommand StoreCommand
            => base.StoreCommand;

        public new IList<ResultSetMapping> ResultSetMappings
            => base.ResultSetMappings;
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
}
