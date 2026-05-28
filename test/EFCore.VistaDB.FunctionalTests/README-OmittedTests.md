# VistaDB FunctionalTests — Omitted SqlServer Test Classes

The SqlServer functional-test suite contains 508 test files. This project ports a substantial
subset (~45 test classes) covering scenarios that are portable across providers. Many SqlServer
test classes are omitted intentionally because they exercise features VistaDB does not support, or
because their implementation is dominated by SqlServer-specific SQL baseline assertions that would
need a parallel set of VistaDB baselines (a separate project on its own).

This document tracks the omitted classes so coverage gaps can be reasoned about explicitly.

## Provider feature gaps that drive omissions

VistaDB does **not** support:

- Azure SQL / Azure Synapse
- Schemas (everything lives in dbo-equivalent default schema)
- Sequences (including the HiLo value-generation strategy)
- Computed columns
- Temporal tables (system-versioned history tables)
- Memory-optimized tables
- `MERGE` statement
- `OUTPUT` clause
- Vector type (`vector(N)`)
- Structural JSON type (`json`)
- User-defined CLR types / `sql_variant`
- `AT TIME ZONE` operator
- `DATEDIFF_BIG`, `DATEFROMPARTS`, `DATETIMEOFFSETFROMPARTS`
- `sp_addextendedproperty` (extended-property metadata)
- Full-text search
- Stored procedures
- Transient-error retry execution strategy
- `geometry` / `geography` types (NTS spatial)

## Omitted by feature dependency

### Sequences / HiLo (no sequence support)

- `GraphUpdates/GraphUpdatesSqlServerHiLoTest.cs`
- `GraphUpdates/GraphUpdatesSqlServerSequenceTest.cs`
- `ManyToManyTrackingProxyGeneratedKeysSqlServerTest.cs` (uses HiLo)
- `SequenceEndToEndTest.cs`
- `Update/StoreValueGenerationSequenceSqlServerTest.cs`
- `Update/StoreValueGenerationSequenceWithoutOutputSqlServerTest.cs`
- `SqlServerValueGenerationConflictTest.cs` (asserts SequenceHiLo conflict warnings)
- `BatchingTest.cs` (asserts `SELECT NEXT VALUE FOR` count in batched inserts)

### Computed columns (no computed-column DDL)

- `ComputedColumnTest.cs`
- `TableSplittingSqlServerTest.cs` (uses `HasComputedColumnSql`)
- `TPTTableSplittingSqlServerTest.cs`

### Temporal tables

- `Migrations/MigrationsSqlServerTest.TemporalTables.cs`
- All temporal-table query and update tests embedded in `Migrations/MigrationsSqlServerTest.cs`

### Memory-optimized tables

- `MemoryOptimizedTablesTest.cs`

### Structural JSON type / Json column mapping

- `JsonTypesSqlServerTest.cs`
- `JsonTypesSqlServerTestBase.cs`
- `JsonTypesCustomMappingSqlServerTest.cs`
- `BadDataJsonDeserializationSqlServerTest.cs` (uses NetTopologySuite too)
- `Update/JsonUpdateSqlServerTest.cs`
- `Update/JsonUpdateSqlServerFixture.cs`
- `Update/JsonUpdateJsonTypeSqlServerTest.cs`
- `Update/JsonUpdateJsonTypeSqlServerFixture.cs`
- `Update/ComplexCollectionJsonUpdateSqlServerTest.cs`
- `Query/AdHocJsonQuerySqlServerTest.cs`
- `Query/AdHocJsonQuerySqlServerTestBase.cs`
- `Query/AdHocJsonQuerySqlServerJsonTypeTest.cs`

### Vector type

- All tests under `Types/` directory referencing `vector(N)`

### Spatial (NetTopologySuite / SqlGeometry / SqlGeography)

- `SpatialSqlServerTest.cs`
- `SpatialSqlServerFixture.cs`

### Stored procedures

- `Update/StoredProcedureUpdateSqlServerTest.cs`
- `Query/FromSqlSprocQuerySqlServerTest.cs`

### Triggers

- `SqlServerTriggersTest.cs`
- `SqlServerQueryTriggersTest.cs`

### Azure SQL / Azure Synapse / SqlAzure

- All tests under `SqlAzure/` directory
- `AzureSynapseTestStoreFactory.cs` / `AzureSynapseTestStore.cs`

### Compatibility-level specific tests

- `ComplexNavigationsQuerySqlServer160Test.cs` (compat-level 160)
- `ComplexNavigationsSharedTypeQuerySqlServer160Test.cs`
- Various `*170*` files keyed on json/vector compat level

### TPC inheritance (requires sequences)

- `TpcManyToManyTrackingSqlServerTest.cs`
- `BulkUpdates/TPCInheritanceBulkUpdatesSqlServerTest.cs`
- `BulkUpdates/TPCInheritanceBulkUpdatesSqlServerFixture.cs`
- `BulkUpdates/TPCFiltersInheritanceBulkUpdatesSqlServerTest.cs`
- `BulkUpdates/TPCFiltersInheritanceBulkUpdatesSqlServerFixture.cs`

### Heavy-data-type stress tests (every type, including unsupported ones)

- `EverythingIsBytesSqlServerTest.cs`
- `EverythingIsStringsSqlServerTest.cs`
- `BuiltInDataTypesSqlServerTest.cs` (column-shape baselines on `[int] [Precision = 10]` style strings)
- `ConvertToProviderTypesSqlServerTest.cs` (asserts SqlServer column shapes)
- `CustomConvertersSqlServerTest.cs` (asserts SqlServer column shapes)

## Omitted due to heavy SQL-baseline coupling

These tests are mostly `AssertSql` / `AssertBaseline` overrides asserting verbatim T-SQL with
`[bracket]` delimiters, `TOP(N)`, `OUTPUT INSERTED.`, `N'literal'`. Porting them means replicating
every baseline against VistaDB's emitted SQL — a separate, large effort.

### Query folder (147 files — entire folder omitted)

Examples (representative subset):

- `Query/NorthwindWhereQuerySqlServerTest.cs` and the rest of `Northwind*` query suite
- `Query/AdHoc*QuerySqlServerTest.cs` (10 files)
- `Query/ComplexNavigations*` (8 files)
- `Query/CompositeKeysQuerySqlServerTest.cs` / `CompositeKeysSplitQuerySqlServerTest.cs`
- `Query/Ef6GroupBySqlServerTest.cs`
- `Query/EntitySplittingQuerySqlServerTest.cs`
- `Query/FromSqlQuerySqlServerTest.cs`
- `Query/Inheritance*QuerySqlServerTest.cs`
- `Query/OwnedQuery*` (~10 files)
- `Query/Tph*` / `Tpt*` / `Tpc*` query variants
- All Associations/ subdirectory tests
- `Query/ComplexTypeQuerySqlServerTest.cs`
- `Query/ComplexNavigationsCollectionsSplitQuerySqlServerTest.cs`

### BulkUpdates folder (15 files — entire folder omitted)

- `NonSharedModelBulkUpdatesSqlServerTest.cs`
- `NorthwindBulkUpdatesSqlServerTest.cs`
- `NorthwindBulkUpdatesSqlServerFixture.cs`
- `TPHInheritanceBulkUpdatesSqlServerTest.cs`
- `TPHFiltersInheritanceBulkUpdatesSqlServerTest.cs`
- `TPTInheritanceBulkUpdatesSqlServerTest.cs`
- `TPTFiltersInheritanceBulkUpdatesSqlServerTest.cs`

### Other heavy-baseline files

- `BuiltInDataTypesSqlServerTest.cs` (column-shape baselines)
- `LoggingSqlServerTest.cs` (asserts logger output against SqlServer-shaped strings)
- `LoadSqlServerTest.cs` — kept as `LoadVistaDBTest.cs` but with all SQL overrides dropped
- `LazyLoadProxySqlServerTest.cs` — kept similarly with SQL overrides dropped
- `FindSqlServerTest.cs` — kept as `FindVistaDBTest.cs` with SQL overrides dropped
- `CommandInterceptionSqlServerTest.cs` (asserts intercepted T-SQL verbatim)
- `SaveChangesInterceptionSqlServerTest.cs` (asserts intercepted T-SQL verbatim)
- `Migrations/MigrationsSqlServerTest.cs` (~3700 lines of MigrationOperation -> T-SQL baselines)
- `Migrations/MigrationsInfrastructureSqlServerTest.cs` (~2100 lines)
- `Migrations/SqlServerMigrationsSqlGeneratorTest.cs`
- `Update/UpdatesSqlServerTestBase.cs`
- `Update/UpdatesSqlServerTest.cs`
- `Update/UpdatesSqlServerTPCTest.cs`
- `Update/UpdatesSqlServerTPTTest.cs`
- `Update/MismatchedKeyTypesSqlServerTest.cs`
- `Update/NonSharedModelUpdatesSqlServerTest.cs`
- `Update/SqlServerUpdateSqlGeneratorTest.cs`
- `Update/StoreValueGenerationIdentitySqlServerTest.cs`
- `Update/StoreValueGenerationIdentityWithoutOutputSqlServerTest.cs`
- `Update/StoreValueGenerationSqlServerFixtureBase.cs`
- `Update/StoreValueGenerationWithoutOutputSqlServerFixture.cs`
- `Update/StoreValueGenerationWithoutOutputSqlServerTestBase.cs`
- `EntitySplittingSqlServerTest.cs` (insert/select baselines + trigger DDL)
- `OptimisticConcurrencySqlServerTest.cs` (depends on F1SqlServerFixture which uses ulong rowversion + decimal column-type baselines)
- `ComplexTypesTrackingSqlServerTest.cs` (depends on SqlServer-only `SqlServerFixtureBase`)
- `ConferencePlannerSqlServerTest.cs` — KEPT (no SQL overrides)
- `MaterializationInterceptionSqlServerTest.cs` — KEPT (the small JSON owned model fragment was dropped, see file comment)
- `Migrations/MigrationsSqlServerTest.NamedDefaultConstraints.cs` (uses `UseNamedDefaultConstraints` SqlServer-specific extension)

## Omitted because of SqlServer-specific infrastructure dependencies

- `SqlServerApiConsistencyTest.cs` (reflects over SqlServer assembly)
- `SqlServerServiceCollectionExtensionsTest.cs` (reflects over SqlServer service registrations)
- `SqlServerDatabaseCreatorTest.cs` (uses `IF EXISTS sys.databases` master-db checks specific to SQL Server)
- `SqlServerConfigPatternsTest.cs` (Northwind-dependent + connection-string config patterns)
- `SqlServerTypeAliasTest.cs` (`CREATE TYPE ... FROM` syntax)
- `Scaffolding/SqlServerDatabaseModelFactoryTest.cs` (~very heavy, baselines on SqlServer system-catalog scan output)
- `Scaffolding/CompiledModelSqlServerTest.cs`
- `ModelBuilding/SqlServerModelBuilderGenericTest.cs` (~70 lines but depends on `SqlServerModelBuilderTestBase.cs` ~2200 lines)
- `ModelBuilding/SqlServerModelBuilderNonGenericTest.cs`
- `ModelBuilding/SqlServerModelBuilderTestBase.cs`
- `ModelBuilding/SqlServerTestModelBuilderExtensions.cs`
- `ModelBuilding101SqlServerTest.cs` (depends on the ModelBuilding base classes above)
- `DbContextPoolingTest.cs` (Northwind-dependent + SqlServer-specific options)
- `CommandConfigurationTest.cs` (asserts `SELECT NEXT VALUE FOR` count in keys-generated-in-batches test)

## Summary count

- Total SqlServer test files: ~508
- VistaDB-ported test classes: ~45 (plus 6 TestUtilities, 4 GraphUpdates subdir)
- Omitted (this document): ~120 classes explicitly listed, plus the bulk of the Query/ and BulkUpdates/ folders (~160 additional)

Many of the omissions are recoverable in future passes: the heavy-baseline tests can be ported in
bulk by either (a) dropping all SQL overrides as we've done here for `Load*` / `LazyLoad*` /
`Find*`, accepting that the suite then exercises only behavioral assertions; or (b) authoring a
parallel set of VistaDB SQL baselines, which is a multi-day effort.
