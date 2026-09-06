// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore.VistaDB.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Metadata.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;
using VistaDB;
using VistaDB.DDA;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore.VistaDB.Migrations;

/// <summary>
///     VistaDB-specific implementation of <see cref="MigrationsSqlGenerator" />.
/// </summary>
/// <remarks>
///     <para>
///         Three-tier strategy:
///     </para>
///     <list type="number">
///         <item>
///             <description>
///                 <b>Tier 1 — SQL</b>: emit standard SQL when VistaDB accepts it. Inherits the base
///                 relational generator's emissions for most operations (CREATE/DROP TABLE, ADD/DROP COLUMN,
///                 primary/foreign keys, unique constraints, indexes, INSERT/UPDATE/DELETE data, raw SQL).
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Tier 2 — DDA managed API</b>: emit a <see cref="VistaDBDdaMigrationCommand" /> carrying
///                 an <see cref="Action{T}" /> over <see cref="IVistaDBDdaAccessor" /> when SQL cannot
///                 express the change (column rename, check constraint, database file create/delete,
///                 alter-column with type change). The command is spliced into the returned
///                 <see cref="MigrationCommand" /> list by the
///                 <see cref="Generate(IReadOnlyList{MigrationOperation}, IModel?, MigrationsSqlGenerationOptions)" />
///                 override using a sentinel-replacement pattern.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Tier 3 — Throw with marker comment</b>: throw a localized <see cref="VistaDBStrings" />
///                 error for operations VistaDB simply does not support (schemas, sequences, temporal tables,
///                 memory-optimized tables, extended properties, computed columns). The verbatim SqlServer
///                 logic is preserved in a <c>/* */</c> marker block above each override for future revival.
///             </description>
///         </item>
///     </list>
///     <para>
///         The service lifetime is <see cref="ServiceLifetime.Scoped" />.
///     </para>
/// </remarks>
public class VistaDBMigrationsSqlGenerator : MigrationsSqlGenerator
{
    private const string DdaSentinelPrefix = "-- VistaDB DDA marker #";

    private readonly Dictionary<string, Action<IVistaDBDdaAccessor>> _pendingDda = new(StringComparer.Ordinal);
    private int _ddaCounter;

    /// <summary>
    ///     Creates a new <see cref="VistaDBMigrationsSqlGenerator" /> instance.
    /// </summary>
    /// <param name="dependencies">Parameter object containing dependencies for this service.</param>
    public VistaDBMigrationsSqlGenerator(MigrationsSqlGeneratorDependencies dependencies)
        : base(dependencies)
    {
    }

    // VistaDB only accepts NO ACTION, CASCADE, SET NULL, SET DEFAULT for FK actions. The relational base
    // emits "RESTRICT" for ReferentialAction.Restrict (line 1775 of MigrationsSqlGenerator.cs in EFCore 10),
    // which VistaDB rejects with: "Expected expression(s): NO ACTION, CASCADE, SET NULL or SET DEFAULT".
    // VistaDB's "NO ACTION" semantics are functionally equivalent to "RESTRICT" — both reject the change
    // when dependent rows exist — so map Restrict → NO ACTION.
    /// <inheritdoc />
    protected override void ForeignKeyAction(ReferentialAction referentialAction, MigrationCommandListBuilder builder)
    {
        if (referentialAction == ReferentialAction.Restrict)
        {
            builder.Append("NO ACTION");
            return;
        }

        base.ForeignKeyAction(referentialAction, builder);
    }

    /// <inheritdoc />
    protected override void Generate(
        AddForeignKeyOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder,
        bool terminate = true)
    {
        if (ShouldSuppressForeignKey(operation, model, out var _))
        {
            // VistaDB: no analog — VistaDB requires FK references to point at the principal table's
            // PRIMARY KEY. SQL Server permits referencing any unique key (alternate key). VistaDB
            // rejects with "Error 601: Invalid Primary key defined in references X". We omit the FK
            // entirely (no SQL emitted). EF Core continues to track the relationship in memory;
            // only the database-level FK enforcement is missing.
            //
            // Do NOT call builder.EndCommand() — that would queue an empty command which VistaDB
            // rejects with "Error 1008: Command text is empty".
            return;
        }

        base.Generate(operation, model, builder, terminate);
    }

    /// <inheritdoc />
    protected override void ForeignKeyConstraint(
        AddForeignKeyOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder)
    {
        // Same suppression check at the inline-FK site (used by CreateTableForeignKeys for the rare
        // case we don't split FKs out via SplitForeignKeysFromCreateTables — defensive).
        if (ShouldSuppressForeignKey(operation, model, out var reason))
        {
            builder
                .Append("CONSTRAINT ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name ?? "FK_suppressed"))
                .Append(" /* VistaDB: suppressed (")
                .Append(reason)
                .Append(") */ PRIMARY KEY ([__never_emit__])");
            // The above placeholder will never execute because we always split FKs out of CREATE TABLE,
            // but if a code path bypasses that, we fail loudly rather than silently.
            return;
        }

        base.ForeignKeyConstraint(operation, model, builder);
    }

    // VistaDB rejects FK references that point at columns other than the principal table's PRIMARY KEY.
    // Returns true if the FK should be suppressed entirely, with `reason` describing why.
    private static bool ShouldSuppressForeignKey(
        AddForeignKeyOperation operation,
        IModel? model,
        out string reason)
    {
        reason = "";
        if (model is null || operation.PrincipalColumns is not { Length: > 0 } principalCols)
        {
            return false;
        }

        var principalTable = model.GetRelationalModel().FindTable(operation.PrincipalTable, operation.PrincipalSchema);
        var pkColumns = principalTable?.PrimaryKey?.Columns;
        if (pkColumns is null)
        {
            return false;
        }

        if (pkColumns.Count != principalCols.Length)
        {
            reason = $"references {principalCols.Length} column(s) but principal PK has {pkColumns.Count}";
            return true;
        }

        for (var i = 0; i < principalCols.Length; i++)
        {
            if (!string.Equals(pkColumns[i].Name, principalCols[i], StringComparison.Ordinal))
            {
                reason = $"references alternate key column [{principalCols[i]}] on {operation.PrincipalTable}, not PK";
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public override IReadOnlyList<MigrationCommand> Generate(
        IReadOnlyList<MigrationOperation> operations,
        IModel? model = null,
        MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default)
    {
        _pendingDda.Clear();
        _ddaCounter = 0;

        // VistaDB rejects inline FK clauses in CREATE TABLE when the referenced table is part of the
        // same batch but not yet created (Error 601: "Invalid Primary key defined in references X").
        // SQL Server is lenient about this; VistaDB is strict. Split each CreateTableOperation's
        // ForeignKeys into separate AddForeignKeyOperation entries appended after all tables exist.
        var rewritten = SplitForeignKeysFromCreateTables(operations);

        var commands = base.Generate(rewritten, model, options);

        if (_pendingDda.Count == 0)
        {
            return commands;
        }

        // Splice DDA commands in place of their sentinel SQL commands.
        var result = new List<MigrationCommand>(commands.Count);
        foreach (var command in commands)
        {
            var text = command.CommandText.TrimStart();
            if (text.StartsWith(DdaSentinelPrefix, StringComparison.Ordinal))
            {
                // Extract the id (everything after the prefix, up to the first whitespace/newline).
                var idStart = DdaSentinelPrefix.Length;
                var idEnd = idStart;
                while (idEnd < text.Length && !char.IsWhiteSpace(text[idEnd]))
                {
                    idEnd++;
                }

                var id = text.Substring(idStart, idEnd - idStart);
                if (_pendingDda.TryGetValue(id, out var action))
                {
                    var sentinelCommand = Dependencies.CommandBuilderFactory.Create()
                        .Append(text)
                        .Build();
                    result.Add(
                        new VistaDBDdaMigrationCommand(
                            sentinelCommand,
                            Dependencies.CurrentContext.Context,
                            Dependencies.Logger,
                            syncAction: action,
                            transactionSuppressed: true));
                    continue;
                }
            }

            result.Add(command);
        }

        _pendingDda.Clear();
        return result;
    }

    // VistaDB rejects forward foreign-key references inside the same CREATE TABLE batch. Move every
    // FK out of CreateTableOperation.ForeignKeys into its own AddForeignKeyOperation appended after
    // all original operations. The base generator then emits each as a separate ALTER TABLE ... ADD
    // CONSTRAINT statement, which VistaDB processes after the referenced tables already exist.
    private static IReadOnlyList<MigrationOperation> SplitForeignKeysFromCreateTables(
        IReadOnlyList<MigrationOperation> operations)
    {
        List<AddForeignKeyOperation>? splitOut = null;

        foreach (var op in operations)
        {
            if (op is CreateTableOperation ct && ct.ForeignKeys.Count > 0)
            {
                splitOut ??= [];
                foreach (var fk in ct.ForeignKeys)
                {
                    // Ensure the FK carries its parent table identity. The model differ typically
                    // sets these already; the null-coalescing is defensive.
                    fk.Schema ??= ct.Schema;
                    fk.Table ??= ct.Name;
                    splitOut.Add(fk);
                }

                ct.ForeignKeys.Clear();
            }
        }

        if (splitOut is null)
        {
            return operations;
        }

        var rewritten = new List<MigrationOperation>(operations.Count + splitOut.Count);
        rewritten.AddRange(operations);
        rewritten.AddRange(splitOut);
        return rewritten;
    }

    /// <summary>
    ///     Emits a DDA-backed command into the SQL stream. Writes a sentinel comment via
    ///     <paramref name="builder" /> and registers <paramref name="ddaAction" /> against that sentinel; the
    ///     outer <see cref="Generate(IReadOnlyList{MigrationOperation}, IModel?, MigrationsSqlGenerationOptions)" />
    ///     swaps the resulting <see cref="MigrationCommand" /> for a <see cref="VistaDBDdaMigrationCommand" /> before
    ///     returning the list.
    /// </summary>
    protected virtual void EndDdaCommand(
        MigrationCommandListBuilder builder,
        Action<IVistaDBDdaAccessor> ddaAction)
    {
        var id = (_ddaCounter++).ToString(CultureInfo.InvariantCulture);
        _pendingDda[id] = ddaAction;
        builder
            .Append(DdaSentinelPrefix)
            .AppendLine(id)
            .EndCommand(suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Generate(MigrationOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        switch (operation)
        {
            case VistaDBCreateDatabaseOperation createDatabaseOperation:
                Generate(createDatabaseOperation, model, builder);
                break;
            case VistaDBDropDatabaseOperation dropDatabaseOperation:
                Generate(dropDatabaseOperation, model, builder);
                break;
            default:
                base.Generate(operation, model, builder);
                break;
        }
    }

    /// <summary>
    ///     Builds a command for the given <see cref="VistaDBCreateDatabaseOperation" />. Emits a Tier-2 DDA
    ///     command that calls <c>IVistaDBDDA.CreateDatabase</c>.
    /// </summary>
    protected virtual void Generate(
        VistaDBCreateDatabaseOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder)
    {
        // VistaDB: no analog — SqlServer's "CREATE DATABASE [name] ON ..." has no VistaDB SQL equivalent;
        // database files are created via the managed DDA surface.
        // Original SqlServer logic preserved below for future revival when VistaDB adds support.
        /*
        builder
            .Append("CREATE DATABASE ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name));
        if (!string.IsNullOrEmpty(operation.FileName))
        {
            // ON (NAME = ..., FILENAME = ...) ...
        }

        builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator).EndCommand(suppressTransaction: true);
        */

        var fileName = operation.FileName;
        EndDdaCommand(
            builder,
            accessor =>
            {
                var path = string.IsNullOrEmpty(fileName) ? accessor.GetDatabaseFilePath() : fileName;
                if (string.IsNullOrEmpty(path))
                {
                    throw new InvalidOperationException(
                        "VistaDBCreateDatabaseOperation requires either FileName to be set or a connection with a 'Data Source'.");
                }

                // Default options: page size 0 (engine default — VistaDB picks; see
                // VistaDBDatabaseCreator for why we don't pass an explicit value), locale 0x0409
                // (en-US), case-insensitive.
                using var created = accessor.Dda.CreateDatabase(
                    path,
                    stayExclusive: false,
                    encryptionKeyString: null,
                    pageSize: 0,
                    LCID: 0x0409,
                    caseSensitive: false);
                created.Close();
            });
    }

    /// <summary>
    ///     Builds a command for the given <see cref="VistaDBDropDatabaseOperation" />. Emits a Tier-2 DDA
    ///     command that deletes the <c>.vdb6</c> file after releasing any open connections.
    /// </summary>
    protected virtual void Generate(
        VistaDBDropDatabaseOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder)
    {
        // VistaDB: no analog — SqlServer's "DROP DATABASE [name]" has no VistaDB SQL equivalent.
        // Original SqlServer logic preserved below for future revival when VistaDB adds support.
        /*
        builder
            .Append("DROP DATABASE ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name))
            .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
            .EndCommand(suppressTransaction: true);
        */

        EndDdaCommand(
            builder,
            accessor =>
            {
                string? path;
                try
                {
                    path = accessor.GetDatabaseFilePath();
                }
                catch (InvalidOperationException)
                {
                    // No data source configured — nothing to delete.
                    return;
                }

                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                // Best-effort: clear pooled DDA connections so the file handle is released.
                try
                {
                    VistaDBEngine.Connections.Clear();
                }
                catch
                {
                    // Ignore — File.Delete below will throw if the handle is still held.
                }

                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            });
    }

    /// <inheritdoc />
    protected override void Generate(EnsureSchemaOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        // VistaDB: no analog — VistaDB has no SCHEMA concept; every object lives in the database's default
        // namespace. Tier-3 throw.
        // Original SqlServer logic preserved below for future revival when VistaDB adds support.
        /*
        if (string.Equals(operation.Name, "dbo", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var stringTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(string));
        builder
            .Append("IF SCHEMA_ID(")
            .Append(stringTypeMapping.GenerateSqlLiteral(operation.Name))
            .Append(") IS NULL EXEC(")
            .Append(stringTypeMapping.GenerateSqlLiteral("CREATE SCHEMA " + Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name)))
            .Append(")")
            .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
            .EndCommand();
        */
        throw new NotSupportedException(VistaDBStrings.SchemasNotSupported(operation.Name, operation.Name));
    }

    /// <inheritdoc />
    protected override void Generate(CreateSequenceOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        // VistaDB: no analog — VistaDB has no SEQUENCE objects. Use IDENTITY columns instead. Tier-3 throw.
        // Original SqlServer logic preserved below for future revival when VistaDB adds support.
        /*
        builder
            .Append("CREATE SEQUENCE ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema));
        // ... AS type, START WITH, INCREMENT BY, MINVALUE, MAXVALUE, CYCLE
        builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator).EndCommand();
        */
        throw new NotSupportedException(VistaDBStrings.SequencesNotSupported);
    }

    /// <inheritdoc />
    protected override void Generate(AlterSequenceOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        // VistaDB: no analog — see CreateSequenceOperation. Tier-3 throw.
        // Original SqlServer logic preserved below for future revival when VistaDB adds support.
        /*
        builder
            .Append("ALTER SEQUENCE ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema));
        // ... INCREMENT BY, MINVALUE, MAXVALUE, CYCLE
        builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator).EndCommand();
        */
        throw new NotSupportedException(VistaDBStrings.SequencesNotSupported);
    }

    /// <inheritdoc />
    protected override void Generate(DropSequenceOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        // VistaDB: no analog — see CreateSequenceOperation. Tier-3 throw.
        // Original SqlServer logic preserved below for future revival when VistaDB adds support.
        /*
        builder
            .Append("DROP SEQUENCE ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema))
            .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
            .EndCommand();
        */
        throw new NotSupportedException(VistaDBStrings.SequencesNotSupported);
    }

    /// <inheritdoc />
    protected override void Generate(RestartSequenceOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        // VistaDB: no analog — see CreateSequenceOperation. Tier-3 throw.
        // Original SqlServer logic preserved below for future revival when VistaDB adds support.
        /*
        builder
            .Append("ALTER SEQUENCE ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema))
            .Append(" RESTART WITH ")
            .Append(IntegerConstant(operation.StartValue))
            .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
            .EndCommand();
        */
        throw new NotSupportedException(VistaDBStrings.SequencesNotSupported);
    }

    /// <inheritdoc />
    protected override void Generate(RenameSequenceOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        // VistaDB: no analog — see CreateSequenceOperation. Tier-3 throw.
        throw new NotSupportedException(VistaDBStrings.SequencesNotSupported);
    }

    /// <inheritdoc />
    protected override void Generate(RenameTableOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        // Tier 1 — VistaDB supports sp_rename for tables. Schemas are unsupported, so reject any schema move.
        if (!string.IsNullOrEmpty(operation.NewSchema) && !string.Equals(operation.NewSchema, operation.Schema, StringComparison.Ordinal))
        {
            throw new NotSupportedException(VistaDBStrings.SchemasNotSupported(operation.Name, operation.NewSchema));
        }

        if (operation.NewName is not null && !string.Equals(operation.NewName, operation.Name, StringComparison.Ordinal))
        {
            var stringTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(string));
            builder
                .Append("EXEC sp_rename ")
                .Append(stringTypeMapping.GenerateSqlLiteral(operation.Name))
                .Append(", ")
                .Append(stringTypeMapping.GenerateSqlLiteral(operation.NewName))
                .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
                .EndCommand();
        }
    }

    /// <inheritdoc />
    protected override void Generate(RenameColumnOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        // VistaDB: SQL-level sp_rename for columns is unreliable — use the managed DDA IVistaDBTable.RenameColumn.
        // Original SqlServer logic preserved below for future revival.
        /*
        var stringTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(string));
        var qualifiedName = Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Table, operation.Schema)
            + "." + Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name);
        builder
            .Append("EXEC sp_rename ")
            .Append(stringTypeMapping.GenerateSqlLiteral(qualifiedName))
            .Append(", ")
            .Append(stringTypeMapping.GenerateSqlLiteral(operation.NewName))
            .Append(", N'COLUMN'")
            .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
            .EndCommand();
        */
        var table = operation.Table;
        var oldName = operation.Name;
        var newName = operation.NewName;
        EndDdaCommand(
            builder,
            accessor =>
            {
                using var database = accessor.OpenDatabase();
                using var schema = database.TableSchema(table);
                schema.AlterColumnName(oldName, newName);
                database.AlterTable(table, schema);
            });
    }

    /// <inheritdoc />
    /// <remarks>
    ///     <para>
    ///         VistaDB treats <c>NULL</c> as a value for uniqueness purposes: a unique index over a nullable
    ///         column rejects the second row with <c>NULL</c> as a duplicate-key collision (engine error 309).
    ///         SQL Server avoids this by emitting a <c>WHERE [col] IS NOT NULL</c> filter on the unique index;
    ///         VistaDB does not accept that syntax (engine error 632 — verified by <c>FilteredIndexProbeTest</c>).
    ///     </para>
    ///     <para>
    ///         Our workaround: when EF Core asks for <c>CREATE UNIQUE INDEX</c> on an index whose model-side
    ///         column metadata reports any indexed column as nullable, emit a <b>non-unique</b> index instead.
    ///         We lose the engine-level uniqueness enforcement for that case, but EF Core's change tracker still
    ///         validates the unique constraint client-side via the model. The alternative — failing the seed —
    ///         would break every TPH optional-1-to-1 navigation (e.g., <c>OptionalSingle1Derived</c> in
    ///         <c>GraphUpdatesTestBase</c>).
    ///     </para>
    /// </remarks>
    protected override void Generate(CreateIndexOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
    {
        if (operation.IsUnique && AnyIndexedColumnIsNullable(operation, model))
        {
            // Clone the operation with IsUnique = false, leave everything else untouched.
            // CreateIndexOperation.IsUnique has a public setter, so we mutate in place then
            // restore — avoids a noisy clone, and the operation is not used past base.Generate.
            operation.IsUnique = false;
            try
            {
                base.Generate(operation, model, builder, terminate);
            }
            finally
            {
                operation.IsUnique = true;
            }
            return;
        }

        base.Generate(operation, model, builder, terminate);
    }

    private static bool AnyIndexedColumnIsNullable(CreateIndexOperation operation, IModel? model)
    {
        if (model is null)
        {
            return false;
        }

        var table = model.GetRelationalModel().FindTable(operation.Table, operation.Schema);
        if (table is null)
        {
            return false;
        }

        foreach (var columnName in operation.Columns)
        {
            if (table.FindColumn(columnName)?.IsNullable == true)
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    protected override void Generate(RenameIndexOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        // VistaDB: no direct SQL — use the DDA IVistaDBTable.RenameIndex.
        // Original SqlServer logic preserved below for future revival.
        /*
        var stringTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(string));
        var qualifiedName = Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Table!, operation.Schema)
            + "." + Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name);
        builder
            .Append("EXEC sp_rename ")
            .Append(stringTypeMapping.GenerateSqlLiteral(qualifiedName))
            .Append(", ")
            .Append(stringTypeMapping.GenerateSqlLiteral(operation.NewName))
            .Append(", N'INDEX'")
            .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
            .EndCommand();
        */
        if (operation.Table is null)
        {
            throw new NotSupportedException(
                "VistaDB cannot rename an index without knowing the table it belongs to. RenameIndexOperation.Table must be set.");
        }

        var table = operation.Table;
        var oldName = operation.Name;
        var newName = operation.NewName;
        EndDdaCommand(
            builder,
            accessor =>
            {
                accessor.WithTable(
                    table,
                    t => t.RenameIndex(oldName, newName),
                    readOnly: false,
                    exclusive: true);
            });
    }

    /// <inheritdoc />
    protected override void Generate(AlterColumnOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        // VistaDB: no ALTER COLUMN that handles type/nullability changes the way SqlServer does. Use the DDA
        // table-schema editor: open the table, mutate IVistaDBTableSchema for the column, and call AlterTable.
        // Original SqlServer logic preserved below for future revival (drop/add column rebuild, identity
        // handling, default-constraint dance).
        /*
        // SqlServer body (see SqlServerMigrationsSqlGenerator.Generate(AlterColumnOperation, ...)) — performs
        // index drop, computed-column rebuild via DROP+ADD, narrowing checks, default-constraint reseat,
        // identity guards, and finally a single "ALTER TABLE ... ALTER COLUMN" command. None of these branches
        // map cleanly to VistaDB's DDA, so the entire body is preserved for future revival.
        */

        if (operation.ComputedColumnSql is not null)
        {
            throw new NotSupportedException(
                VistaDBStrings.ComputedColumnsNotSupported(operation.Name, operation.Table));
        }

        var table = operation.Table;
        var columnName = operation.Name;
        var columnType = operation.ColumnType
            ?? GetColumnType(operation.Schema, operation.Table, operation.Name, operation, model);
        var isNullable = operation.IsNullable;
        var defaultValueSql = operation.DefaultValueSql;
        var defaultValue = operation.DefaultValue;
        var maxLength = operation.MaxLength;

        EndDdaCommand(
            builder,
            accessor =>
            {
                using var database = accessor.OpenDatabase();
                using var tableSchema = database.TableSchema(table);

                // Try to find the existing column by name and alter it; if absent, fall back to AddColumn.
                IVistaDBColumnAttributes? existing = null;
                for (var i = 0; i < tableSchema.ColumnCount; i++)
                {
                    var col = tableSchema[i];
                    if (string.Equals(col.Name, columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        existing = col;
                        break;
                    }
                }

                var vistaDbType = TryParseVistaDbType(columnType);
                if (existing is null)
                {
                    tableSchema.AddColumn(columnName, vistaDbType, maxLength ?? 0);
                }
                else
                {
                    tableSchema.AlterColumnType(columnName, vistaDbType, maxLength ?? 0);
                }

                tableSchema.DefineColumnAttributes(columnName, isNullable, false, false, false, null);

                if (defaultValueSql is not null)
                {
                    tableSchema.DefineDefaultValue(columnName, defaultValueSql, false, null);
                }
                else if (defaultValue is not null)
                {
                    tableSchema.DefineDefaultValue(columnName, RenderDdlDefault(defaultValue), false, null);
                }

                database.AlterTable(table, tableSchema);
            });
    }

    /// <inheritdoc />
    protected override void Generate(AddCheckConstraintOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        // VistaDB: use the DDA IVistaDBTable.CreateConstraint instead of inline ALTER TABLE ADD CONSTRAINT.
        // Original SqlServer logic preserved below for future revival.
        /*
        base.Generate(operation, model, builder);
        */
        var table = operation.Table;
        var name = operation.Name;
        var sql = operation.Sql;
        EndDdaCommand(
            builder,
            accessor =>
            {
                accessor.WithTable(
                    table,
                    t => t.CreateConstraint(name, sql, /* description */ null!, /* errorOnInsert */ true, /* errorOnUpdate */ true, /* errorOnDelete */ false),
                    readOnly: false,
                    exclusive: true);
            });
    }

    /// <inheritdoc />
    protected override void Generate(DropCheckConstraintOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        // VistaDB: use IVistaDBTable.DropConstraint via DDA.
        // Original SqlServer logic preserved below for future revival.
        /*
        base.Generate(operation, model, builder);
        */
        var table = operation.Table;
        var name = operation.Name;
        EndDdaCommand(
            builder,
            accessor =>
            {
                accessor.WithTable(
                    table,
                    t => t.DropConstraint(name),
                    readOnly: false,
                    exclusive: true);
            });
    }

    /// <inheritdoc />
    protected override void Generate(CreateTableOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
    {
        // Tier 3 guard — temporal tables, memory-optimized tables.
        // The IsTemporal/MemoryOptimized annotations are SqlServer-specific; on a VistaDB model the
        // SqlServerAnnotationNames constants aren't available here, so we probe by literal prefix.
        foreach (var ann in operation.GetAnnotations())
        {
            if (string.Equals(ann.Name, "SqlServer:IsTemporal", StringComparison.Ordinal)
                && ann.Value is bool isTemporal && isTemporal)
            {
                throw new NotSupportedException(VistaDBStrings.TemporalTablesNotSupported(operation.Name));
            }

            if (string.Equals(ann.Name, "SqlServer:MemoryOptimized", StringComparison.Ordinal)
                && ann.Value is bool memOpt && memOpt)
            {
                throw new NotSupportedException(VistaDBStrings.MemoryOptimizedTablesNotSupported);
            }
        }

        // Tier 3 guard — computed columns.
        foreach (var column in operation.Columns)
        {
            if (column.ComputedColumnSql is not null)
            {
                throw new NotSupportedException(VistaDBStrings.ComputedColumnsNotSupported(column.Name, operation.Name));
            }
        }

        base.Generate(operation, model, builder, terminate);
    }

    /// <inheritdoc />
    protected override void Generate(AddColumnOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate)
    {
        if (operation.ComputedColumnSql is not null)
        {
            // Tier 3 — VistaDB has no computed columns.
            // Original SqlServer logic preserved below for future revival.
            /*
            base.Generate(operation, model, builder, terminate);
            */
            throw new NotSupportedException(VistaDBStrings.ComputedColumnsNotSupported(operation.Name, operation.Table));
        }

        if (IsIdentity(operation))
        {
            // Strip DefaultValue when IDENTITY is set — VistaDB, like SqlServer, can't have both.
            operation.DefaultValue = null;
        }

        base.Generate(operation, model, builder, terminate);
    }

    /// <inheritdoc />
    protected override void ColumnDefinition(
        string? schema,
        string table,
        string name,
        ColumnOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder)
    {
        if (operation.ComputedColumnSql is not null)
        {
            throw new NotSupportedException(VistaDBStrings.ComputedColumnsNotSupported(name, table));
        }

        var columnType = operation.ColumnType ?? GetColumnType(schema, table, name, operation, model);

        builder
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(name))
            .Append(" ")
            .Append(columnType);

        if (operation.Collation != null)
        {
            builder
                .Append(" COLLATE ")
                .Append(operation.Collation);
        }

        builder.Append(operation.IsNullable ? " NULL" : " NOT NULL");

        DefaultValue(operation.DefaultValue, operation.DefaultValueSql, columnType, builder);

        if (IsIdentity(operation))
        {
            var (seed, increment) = GetIdentitySeedIncrement(operation);
            builder
                .Append(" IDENTITY(")
                .Append(seed.ToString(CultureInfo.InvariantCulture))
                .Append(", ")
                .Append(increment.ToString(CultureInfo.InvariantCulture))
                .Append(")");
        }
    }

    /// <summary>
    ///     Renders a column default.
    /// </summary>
    /// <remarks>
    ///     VistaDB parses DDL more strictly than it parses expressions. The bit type mapping renders
    ///     <c>CAST(0 AS bit)</c>, which is what SQL Server wants and what VistaDB itself accepts inside a
    ///     query — but its DDL parser rejects a CAST in a DEFAULT clause with error 285, "invalid
    ///     expression", taking the whole ALTER TABLE down with error 120. Booleans are therefore written
    ///     as bare <c>0</c> and <c>1</c> here, and only here; everywhere else the mapping is unchanged.
    /// </remarks>
    protected override void DefaultValue(
        object? defaultValue,
        string? defaultValueSql,
        string? columnType,
        MigrationCommandListBuilder builder)
    {
        if (defaultValueSql is null && defaultValue is bool flag)
        {
            builder.Append(" DEFAULT ").Append(flag ? "1" : "0");
            return;
        }

        base.DefaultValue(defaultValue, defaultValueSql, columnType, builder);
    }

    /// <summary>
    ///     A default value as VistaDB wants it written in DDL. Shared by the SQL and DDA paths so an
    ///     added column and an altered one cannot disagree about what <c>false</c> looks like.
    /// </summary>
    protected static string RenderDdlDefault(object defaultValue)
        => defaultValue switch
        {
            // "False" is what ToString gives, and VistaDB will not take it.
            bool flag => flag ? "1" : "0",
            _ => defaultValue.ToString() ?? string.Empty,
        };

    /// <summary>
    ///     Determines whether <paramref name="operation" /> is annotated for VistaDB IDENTITY.
    /// </summary>
    protected virtual bool IsIdentity(ColumnOperation operation)
    {
        var strategy = operation[VistaDBAnnotationNames.ValueGenerationStrategy];
        if (strategy is VistaDBValueGenerationStrategy vgs)
        {
            return vgs == VistaDBValueGenerationStrategy.IdentityColumn;
        }

        // Also accept the raw Identity annotation if set explicitly.
        return operation[VistaDBAnnotationNames.Identity] is not null;

        // NOTE: A naive "any int non-nullable PK column is IDENTITY" heuristic was tried here and
        // reverted — it broke fixtures that seed with explicit Id values (e.g. FieldsOnlyLoad seeds
        // entity instances with pre-assigned Ids, which conflicts with IDENTITY columns). The proper
        // fix is to implement VistaDBValueGenerationStrategyConvention analogous to
        // SqlServerValueGenerationStrategyConvention.cs, which sets the model-level default to
        // IdentityColumn and propagates it to integer PK properties — together with respecting the
        // property's ValueGenerated flag so explicitly OnNone properties aren't auto-IDENTITY'd.
        // That's the next session's first task.
    }

    private static (long Seed, int Increment) GetIdentitySeedIncrement(ColumnOperation operation)
    {
        var seedObj = operation[VistaDBAnnotationNames.IdentitySeed];
        var incrementObj = operation[VistaDBAnnotationNames.IdentityIncrement];

        long seed = seedObj switch
        {
            long l => l,
            int i => i,
            string s when long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 1L
        };

        int increment = incrementObj switch
        {
            int i => i,
            long l => (int)l,
            string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 1
        };

        return (seed, increment);
    }

    private static VistaDBType TryParseVistaDbType(string columnType)
    {
        // Normalise: strip everything from the first parenthesis ("nvarchar(50)" -> "nvarchar").
        var typeName = columnType;
        var paren = typeName.IndexOf('(');
        if (paren >= 0)
        {
            typeName = typeName.Substring(0, paren);
        }

        typeName = typeName.Trim();

        return typeName.ToLowerInvariant() switch
        {
            "bit" => VistaDBType.Bit,
            "tinyint" => VistaDBType.TinyInt,
            "smallint" => VistaDBType.SmallInt,
            "int" or "integer" => VistaDBType.Int,
            "bigint" => VistaDBType.BigInt,
            "real" => VistaDBType.Real,
            "float" => VistaDBType.Float,
            "decimal" or "numeric" => VistaDBType.Decimal,
            "money" => VistaDBType.Money,
            "smallmoney" => VistaDBType.SmallMoney,
            "datetime" => VistaDBType.DateTime,
            "smalldatetime" => VistaDBType.SmallDateTime,
            "date" => VistaDBType.DateTime,
            "time" => VistaDBType.DateTime,
            "datetimeoffset" => VistaDBType.DateTime,
            "uniqueidentifier" => VistaDBType.UniqueIdentifier,
            "image" or "varbinary" or "binary" => VistaDBType.VarBinary,
            "text" => VistaDBType.Text,
            "ntext" => VistaDBType.NText,
            "char" => VistaDBType.Char,
            "varchar" => VistaDBType.VarChar,
            "nchar" => VistaDBType.NChar,
            "nvarchar" => VistaDBType.NVarChar,
            "timestamp" or "rowversion" => VistaDBType.Timestamp,
            _ => VistaDBType.NVarChar
        };
    }

    /// <summary>
    ///     VistaDB requires <c>SET IDENTITY_INSERT [Table] ON/OFF</c> around any seed-data <c>INSERT</c>
    ///     that may contain explicit values for IDENTITY columns. Mirror SqlServer's
    ///     <c>Generate(InsertDataOperation)</c> override exactly, minus the
    ///     <c>IF EXISTS ([sys].[identity_columns] …)</c> gate (VistaDB has no such catalog table) and
    ///     minus the <c>EXEC(N'…')</c> idempotent wrapper (VistaDB has no <c>EXEC</c> keyword).
    /// </summary>
    /// <inheritdoc />
    protected override void Generate(
        InsertDataOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder,
        bool terminate = true)
    {
        GenerateIdentityInsert(builder, operation, on: true, model);

        var sqlBuilder = new StringBuilder();
        foreach (var modificationCommand in GenerateModificationCommands(operation, model))
        {
            SqlGenerator.AppendInsertOperation(sqlBuilder, modificationCommand, commandPosition: 0);
        }

        builder.Append(sqlBuilder.ToString());

        GenerateIdentityInsert(builder, operation, on: false, model);

        if (terminate)
        {
            EndStatement(builder);
        }
    }

    /// <summary>
    ///     Emits <c>SET IDENTITY_INSERT [&lt;table&gt;] ON</c> (or <c>OFF</c>) for the table targeted by
    ///     <paramref name="operation" />. Unconditional — unlike SqlServer we don't gate on
    ///     <c>sys.identity_columns</c> because that catalog doesn't exist in VistaDB; the SQL is harmless
    ///     when the target table has no identity column.
    /// </summary>
    private void GenerateIdentityInsert(
        MigrationCommandListBuilder builder,
        InsertDataOperation operation,
        bool on,
        IModel? model)
        => builder
            .Append("SET IDENTITY_INSERT ")
            .Append(
                Dependencies.SqlGenerationHelper.DelimitIdentifier(
                    operation.Table,
                    operation.Schema ?? model?.GetDefaultSchema()))
            .Append(on ? " ON" : " OFF")
            .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
}
