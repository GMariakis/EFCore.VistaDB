// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.VistaDB.Metadata.Internal;
using VistaDB.DDA;
using VistaDBClient = global::VistaDB.Provider.VistaDBConnection;

namespace Microsoft.EntityFrameworkCore.VistaDB.Scaffolding.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
/// <remarks>
///     <para>
///         VistaDB scaffolding model factory. Uses <c>INFORMATION_SCHEMA</c> (which VistaDB exposes) plus
///         the VistaDB Direct Data Access (DDA) API (<see cref="IVistaDBDatabase" />, <see cref="IVistaDBTableSchema" />)
///         for richer metadata that <c>INFORMATION_SCHEMA</c> does not expose — most importantly identity
///         columns, index uniqueness/primary flags, and foreign-key cascade behavior.
///     </para>
///     <para>
///         VistaDB has no schemas (everything is in the implicit <c>dbo</c> namespace), no sequences, no
///         temporal tables, no memory-optimized tables, no computed columns, no sparse columns, no
///         vector/structural-JSON/UDT/sql_variant types, no views and no DML triggers surfaced through
///         scaffolding. The corresponding helper methods from the SqlServer factory are preserved in marker
///         blocks but the runtime path returns empty collections.
///     </para>
/// </remarks>
public class VistaDBDatabaseModelFactory : DatabaseModelFactory
{
    private static readonly ISet<string> DateTimePrecisionTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "datetimeoffset",
        "datetime2",
        "time"
    };

    private static readonly ISet<string> MaxLengthRequiredTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "binary",
        "varbinary",
        "char",
        "varchar",
        "nchar",
        "nvarchar"
    };

    private readonly IDiagnosticsLogger<DbLoggerCategory.Scaffolding> _logger;
    private readonly IRelationalTypeMappingSource _typeMappingSource;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBDatabaseModelFactory(
        IDiagnosticsLogger<DbLoggerCategory.Scaffolding> logger,
        IRelationalTypeMappingSource typeMappingSource)
    {
        _logger = logger;
        _typeMappingSource = typeMappingSource;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override DatabaseModel Create(string connectionString, DatabaseModelFactoryOptions options)
    {
        // Default to MultiProcessReadWrite so the same file can be opened concurrently by other
        // DbContext instances (or other processes) without forcing scaffold-time exclusivity. Earlier
        // attempts forced SingleProcessReadWrite here to align with what VistaDBClient was registering
        // in the lock-manager — that was the wrong fix. The real win is that scaffolding does not
        // need a SQL connection at all now that table/column/index/FK enumeration goes through DDA
        // (see Create(DbConnection, options) below: the SQL connection is never opened in the scaffold
        // path). MultiProcessReadWrite for the DDA handle therefore has no peer to conflict with.
        var builder = new global::VistaDB.Provider.VistaDBConnectionStringBuilder(connectionString);
        if (!builder.ContainsKey("Open Mode"))
        {
            builder.OpenMode = global::VistaDB.VistaDBDatabaseOpenMode.MultiProcessReadWrite;
        }

        using var connection = new VistaDBClient(builder.ConnectionString);
        return Create(connection, options);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override DatabaseModel Create(DbConnection connection, DatabaseModelFactoryOptions options)
    {
        // Touch the logger and the unused fields to keep the analyzers quiet without dropping the field —
        // future revisions of this factory will route progress through these (mirroring the SqlServer factory).
        _ = _logger;

        var databaseModel = new DatabaseModel();

        // DELIBERATE: we do NOT open the SQL connection in the scaffold path. All metadata —
        // tables, columns, indexes, foreign keys — is obtained through DDA. Opening a SQL connection
        // here used to be necessary for INFORMATION_SCHEMA queries (which VistaDB doesn't support
        // anyway, error 627), and the now-unused open caused a lock-manager mode conflict (error 219)
        // when DDA's MultiProcessReadWrite then tried to register alongside it. We only need the
        // connection for two pieces of information: the database file path (read by OpenDda below
        // from connection.DataSource or the parsed connection string) and the database name (which
        // we derive from the same file path so the connection can stay closed throughout).
        var dataSource = ExtractDataSource(connection);
        databaseModel.DatabaseName = string.IsNullOrEmpty(dataSource)
            ? connection.Database
            : Path.GetFileNameWithoutExtension(dataSource);
        databaseModel.DefaultSchema = GetDefaultSchema();

        var tableList = options.Tables.ToList();
        var requestedTableNames = new HashSet<string>(
            tableList.Select(ExtractTableName),
            StringComparer.OrdinalIgnoreCase);

        // VistaDB: no analog — VistaDB has no CREATE SEQUENCE, so no sequence enumeration is needed.
        // Original SqlServer logic preserved below for future revival when VistaDB adds support.
        /*
        if (SupportsSequences())
        {
            GetSequences(connection, databaseModel, schemaFilter, typeAliases);
        }
        */

        using var dda = OpenDda(connection);

        GetTables(connection, dda, databaseModel, requestedTableNames);

        foreach (var requested in tableList)
        {
            var name = ExtractTableName(requested);
            if (!databaseModel.Tables.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                // VistaDB: no logger.MissingTableWarning analog wired yet — emit a Debug write so the
                // missing-table case is still observable. Original SqlServer logic preserved.
                /*
                _logger.MissingTableWarning(requested);
                */
                Debug.WriteLine($"VistaDB scaffolder: requested table '{requested}' was not found.");
            }
        }

        return databaseModel;
    }

    /// <summary>
    ///     Pull the .vdb6 file path off the connection without requiring it to be open.
    ///     Mirrors the lookup OpenDda() does — both must agree, so factor it once.
    /// </summary>
    private static string ExtractDataSource(DbConnection connection)
    {
        var dataSource = connection.DataSource;
        if (!string.IsNullOrEmpty(dataSource))
        {
            return dataSource;
        }

        var csb = new DbConnectionStringBuilder { ConnectionString = connection.ConnectionString };
        if (csb.TryGetValue("Data Source", out var ds) && ds is string s)
        {
            return s;
        }

        return string.Empty;
    }

    /// <summary>
    ///     VistaDB does not support schemas; every object lives in the implicit <c>dbo</c> namespace.
    /// </summary>
    // VistaDB: no analog — there is no SCHEMA_NAME() / sys.schemas. Always return null so the relational layer
    // treats the implicit "dbo" schema as the default, matching how the rest of the provider models it.
    // Original SqlServer logic preserved below for future revival when VistaDB adds schemas.
    /*
    private string? GetDefaultSchema(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT SCHEMA_NAME();";
        if (command.ExecuteScalar() is string schema)
        {
            _logger.DefaultSchemaFound(schema);
            return schema;
        }
        return null;
    }
    */
    private static string? GetDefaultSchema()
        => null;

    private static string ExtractTableName(string input)
    {
        // Strip an optional "schema." prefix that the user might have typed.
        var lastDot = input.LastIndexOf('.');
        return lastDot < 0 ? input : input[(lastDot + 1)..];
    }

    private static IVistaDBDatabase OpenDda(DbConnection connection)
    {
        var dataSource = connection.DataSource;
        if (string.IsNullOrEmpty(dataSource))
        {
            var csb = new DbConnectionStringBuilder { ConnectionString = connection.ConnectionString };
            if (csb.TryGetValue("Data Source", out var ds) && ds is string s)
            {
                dataSource = s;
            }
        }

        if (string.IsNullOrEmpty(dataSource))
        {
            throw new InvalidOperationException(
                "VistaDB scaffolder: the connection has no 'Data Source' (database file path).");
        }

        string? password = null;
        try
        {
            var csb = new DbConnectionStringBuilder { ConnectionString = connection.ConnectionString };
            if (csb.TryGetValue("Password", out var pwd) && pwd is string s && s.Length > 0)
            {
                password = s;
            }
        }
        catch
        {
            // Ignored — fall through with no password.
        }

        var ddaRoot = global::VistaDB.DDA.VistaDBEngine.Connections.OpenDDA();
        // VistaDB allows the DDA and the ADO.NET path to share the same file within a single process.
        // SharedReadOnly works when no other handle exists; SingleProcessReadWrite conflicts with the
        // ADO.NET connection's SingleProcessReadWrite and blocks INFORMATION_SCHEMA. Use
        // MultiProcessReadWrite which allows concurrent access including with INFORMATION_SCHEMA queries.
        // The "another process" error that MultiProcessReadWrite raised earlier was from the EnsureCreated
        // DDA handle — now that the test creates the DB differently, MultiProcessReadWrite should succeed.
        // MultiProcessReadWrite: scaffolding doesn't open a SQL connection alongside this (Create()
        // above is now DDA-only) so there is no in-process peer mode to conflict with. Cross-process
        // sharing is preserved — another instance of the same .vdb6 opened by a runtime DbContext
        // elsewhere remains compatible.
        return ddaRoot.OpenDatabase(dataSource, global::VistaDB.VistaDBDatabaseOpenMode.MultiProcessReadWrite, password);
    }

    private void GetTables(
        DbConnection connection,
        IVistaDBDatabase dda,
        DatabaseModel databaseModel,
        ISet<string> requestedTableNames)
    {
        // Enumerate the user tables via DDA. VistaDB doesn't ship the SQL Server-style
        // INFORMATION_SCHEMA views (a query against them produces error 627 "Invalid schema name.
        // DBO must be used instead of: INFORMATION_SCHEMA"). DDA's GetTableNames() returns just the
        // user tables — system tables aren't included, which is what we want.
        var tableNames = new List<string>();
        foreach (string name in dda.GetTableNames())
        {
            if (requestedTableNames.Count > 0 && !requestedTableNames.Contains(name))
            {
                continue;
            }

            tableNames.Add(name);
        }

        tableNames.Sort(StringComparer.OrdinalIgnoreCase);

        var tables = new List<DatabaseTable>(tableNames.Count);
        var tablesByName = new Dictionary<string, DatabaseTable>(StringComparer.OrdinalIgnoreCase);
        var columnOrderByTable = new Dictionary<string, List<DatabaseColumn>>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in tableNames)
        {
            var table = new DatabaseTable
            {
                Database = databaseModel,
                Name = name,
                Schema = null
            };

            tables.Add(table);
            tablesByName[name] = table;
        }

        foreach (var table in tables)
        {
            var ordered = GetColumns(dda, table);
            columnOrderByTable[table.Name] = ordered;
        }

        foreach (var table in tables)
        {
            GetIndexes(dda, table, columnOrderByTable[table.Name]);
        }

        foreach (var table in tables)
        {
            GetForeignKeys(dda, table, tablesByName, columnOrderByTable);
        }

        // VistaDB: no analog — DML triggers are not surfaced via scaffolding. Original SqlServer logic
        // preserved for future revival when VistaDB adds support.
        /*
        if (SupportsTriggers())
        {
            GetTriggers(connection, tables, tableFilterSql);
        }
        */

        foreach (var table in tables)
        {
            databaseModel.Tables.Add(table);
        }
    }

    /// <summary>
    ///     Reads columns from the VistaDB table schema and returns the ordered list of <see cref="DatabaseColumn" />
    ///     mirroring the schema column order — callers map <see cref="IVistaDBKeyColumn.RowIndex" /> back to a column
    ///     via this list.
    /// </summary>
    private List<DatabaseColumn> GetColumns(IVistaDBDatabase dda, DatabaseTable table)
    {
        var orderedColumns = new List<DatabaseColumn>();

        using var schema = dda.TableSchema(table.Name);

        var identityNames = CollectIdentityColumnNames(schema.Identities);
        var defaultExprByColumn = CollectDefaultValueExpressions(schema.DefaultValues);

        for (var i = 0; i < schema.ColumnCount; i++)
        {
            var col = schema[i];
            if (col is null)
            {
                orderedColumns.Add(null!);
                continue;
            }

            if (col.IsSystem)
            {
                // Keep the slot so RowIndex stays aligned.
                orderedColumns.Add(null!);
                continue;
            }

            var typeName = col.Type.ToString().ToLowerInvariant();
            var storeType = GetStoreType(typeName, col.MaxLength);
            var isIdentity = identityNames.Contains(col.Name);
            defaultExprByColumn.TryGetValue(col.Name, out var defaultValueSql);

            var column = new DatabaseColumn
            {
                Table = table,
                Name = col.Name,
                StoreType = storeType,
                IsNullable = col.AllowNull,
                DefaultValueSql = string.IsNullOrEmpty(defaultValueSql) ? null : defaultValueSql,
                DefaultValue = TryParseClrDefault(typeName, defaultValueSql),
                Comment = string.IsNullOrEmpty(col.Description) ? null : col.Description,
                ValueGenerated = isIdentity ? ValueGenerated.OnAdd : (ValueGenerated?)null
            };

            if (isIdentity)
            {
                column[VistaDBAnnotationNames.ValueGenerationStrategy] = VistaDBValueGenerationStrategy.IdentityColumn;
            }

            table.Columns.Add(column);
            orderedColumns.Add(column);
        }

        return orderedColumns;
    }

    private static void GetIndexes(IVistaDBDatabase dda, DatabaseTable table, List<DatabaseColumn> orderedColumns)
    {
        using var schema = dda.TableSchema(table.Name);

        foreach (var index in schema.Indexes)
        {
            if (index.Temporary || index.FullTextSearch || index.FKConstraint)
            {
                continue;
            }

            var columns = ResolveIndexColumns(orderedColumns, index.KeyStructure);
            if (columns.Count == 0)
            {
                continue;
            }

            if (index.Primary)
            {
                var pk = new DatabasePrimaryKey
                {
                    Table = table,
                    Name = index.Name
                };

                foreach (var col in columns)
                {
                    pk.Columns.Add(col);
                }

                table.PrimaryKey = pk;
                continue;
            }

            if (index.Unique)
            {
                var uc = new DatabaseUniqueConstraint
                {
                    Table = table,
                    Name = index.Name
                };

                foreach (var col in columns)
                {
                    uc.Columns.Add(col);
                }

                table.UniqueConstraints.Add(uc);
                continue;
            }

            var dbIndex = new DatabaseIndex
            {
                Table = table,
                Name = index.Name,
                IsUnique = index.Unique,
                Filter = null
            };

            for (var i = 0; i < columns.Count; i++)
            {
                dbIndex.Columns.Add(columns[i]);
                // KeyStructure preserves direction; mirror it.
                dbIndex.IsDescending.Add(index.KeyStructure[i].Descending);
            }

            table.Indexes.Add(dbIndex);
        }
    }

    private static void GetForeignKeys(
        IVistaDBDatabase dda,
        DatabaseTable table,
        IReadOnlyDictionary<string, DatabaseTable> tablesByName,
        IReadOnlyDictionary<string, List<DatabaseColumn>> columnOrderByTable)
    {
        using var schema = dda.TableSchema(table.Name);

        foreach (var rel in schema.ForeignKeys)
        {
            // Only process relationships where this table is the foreign (child) side.
            if (!string.Equals(rel.ForeignTable, table.Name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!tablesByName.TryGetValue(rel.PrimaryTable, out var principalTable))
            {
                // Principal table is outside the requested set — skip.
                continue;
            }

            if (!columnOrderByTable.TryGetValue(table.Name, out var foreignOrder))
            {
                continue;
            }

            var foreignColumnNames = SplitKeyExpression(rel.ForeignKey);
            if (foreignColumnNames.Count == 0)
            {
                continue;
            }

            // VistaDB's relationship metadata only stores the foreign-side column expression — the principal
            // side is implicitly the principal table's primary key.
            var principalColumns = principalTable.PrimaryKey?.Columns;
            if (principalColumns is null || principalColumns.Count != foreignColumnNames.Count)
            {
                continue;
            }

            var fk = new DatabaseForeignKey
            {
                Table = table,
                Name = rel.Name,
                PrincipalTable = principalTable,
                OnDelete = ConvertReferentialIntegrity(rel.DeleteIntegrity)
            };

            var invalid = false;
            foreach (var name in foreignColumnNames)
            {
                var col = foreignOrder.FirstOrDefault(
                    c => c is not null && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
                if (col is null)
                {
                    invalid = true;
                    break;
                }

                fk.Columns.Add(col);
            }

            if (invalid)
            {
                continue;
            }

            foreach (var pc in principalColumns)
            {
                fk.PrincipalColumns.Add(pc);
            }

            if (fk.Columns.SequenceEqual(fk.PrincipalColumns))
            {
                // Reflexive — skip, mirroring SqlServer factory behavior.
                continue;
            }

            table.ForeignKeys.Add(fk);
        }
    }

    private static IReadOnlyList<DatabaseColumn> ResolveIndexColumns(
        List<DatabaseColumn> orderedColumns,
        IVistaDBKeyColumn[] keyStructure)
    {
        var result = new List<DatabaseColumn>(keyStructure.Length);
        foreach (var kc in keyStructure)
        {
            if (kc.RowIndex < 0 || kc.RowIndex >= orderedColumns.Count)
            {
                return Array.Empty<DatabaseColumn>();
            }

            var col = orderedColumns[kc.RowIndex];
            if (col is null)
            {
                return Array.Empty<DatabaseColumn>();
            }

            result.Add(col);
        }

        return result;
    }

    private static List<string> SplitKeyExpression(string? keyExpression)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(keyExpression))
        {
            return result;
        }

        // VistaDB key/foreign-key expressions are typically of the form "Column1;Column2"
        // or "Column1 ASC;Column2 DESC". Strip direction tokens and brackets/quotes.
        foreach (var raw in keyExpression.Split([';', ','], StringSplitOptions.RemoveEmptyEntries))
        {
            var token = raw.Trim();
            var space = token.IndexOf(' ');
            if (space >= 0)
            {
                token = token[..space].Trim();
            }

            token = token.Trim('[', ']', '"', '`');
            if (token.Length > 0)
            {
                result.Add(token);
            }
        }

        return result;
    }

    private static HashSet<string> CollectIdentityColumnNames(IVistaDBIdentityCollection identities)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in identities)
        {
            if (entry?.ColumnName is { Length: > 0 } name)
            {
                set.Add(name);
            }
        }

        return set;
    }

    private static Dictionary<string, string> CollectDefaultValueExpressions(IVistaDBDefaultValueCollection defaults)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in defaults)
        {
            if (entry?.ColumnName is { Length: > 0 } name && entry.Expression is { } expr)
            {
                map[name] = expr;
            }
        }

        return map;
    }

    private static ReferentialAction? ConvertReferentialIntegrity(VistaDBReferentialIntegrity action)
        => action switch
        {
            VistaDBReferentialIntegrity.None => ReferentialAction.NoAction,
            VistaDBReferentialIntegrity.Cascade => ReferentialAction.Cascade,
            VistaDBReferentialIntegrity.SetNull => ReferentialAction.SetNull,
            VistaDBReferentialIntegrity.SetDefault => ReferentialAction.SetDefault,
            _ => null
        };

    private static string GetStoreType(string typeName, int maxLength)
    {
        if (typeName is "decimal" or "numeric")
        {
            return typeName;
        }

        if (DateTimePrecisionTypes.Contains(typeName))
        {
            return typeName;
        }

        if (MaxLengthRequiredTypes.Contains(typeName))
        {
            if (maxLength <= 0)
            {
                return $"{typeName}(max)";
            }

            if (typeName is "nvarchar" or "nchar")
            {
                var charLen = maxLength / 2;
                if (charLen > 0)
                {
                    return $"{typeName}({charLen})";
                }
            }

            return $"{typeName}({maxLength})";
        }

        return typeName;
    }

    private object? TryParseClrDefault(string dataTypeName, string? defaultValueSql)
    {
        if (string.IsNullOrWhiteSpace(defaultValueSql))
        {
            return null;
        }

        var mapping = _typeMappingSource.FindMapping(dataTypeName);
        if (mapping is null)
        {
            return null;
        }

        var trimmed = defaultValueSql.Trim();
        while (trimmed.StartsWith('(') && trimmed.EndsWith(')'))
        {
            trimmed = trimmed[1..^1].Trim();
        }

        if (trimmed.Equals("NULL", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var clrType = mapping.ClrType;
        if (clrType == typeof(bool) && int.TryParse(trimmed, out var i))
        {
            return i != 0;
        }

        if (clrType.IsNumeric())
        {
            try
            {
                return Convert.ChangeType(trimmed, clrType, CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        if (trimmed.StartsWith('\'') && trimmed.EndsWith('\''))
        {
            var inner = trimmed[1..^1];
            if (clrType == typeof(string))
            {
                return inner;
            }

            if (clrType == typeof(Guid) && Guid.TryParse(inner, out var guid))
            {
                return guid;
            }

            if (clrType == typeof(DateTime) && DateTime.TryParse(inner, CultureInfo.InvariantCulture, out var dt))
            {
                return dt;
            }
        }

        return null;
    }
}
