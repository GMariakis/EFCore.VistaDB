# Omitted SqlServer test files

These tests from `test/EFCore.SqlServer.Tests/` were intentionally not ported to
`test/EFCore.VistaDB.Tests/`. Each is a SqlServer-only feature that has no analog in
VistaDB. Documented here so test-coverage gaps are deliberate, not accidental.

| SqlServer test file | Reason for omission |
|---|---|
| `Extensions/AzureSqlDbContextOptionsExtensionsTest.cs` | VistaDB has no Azure SQL / Azure Synapse engine. |
| `Metadata/Conventions/SqlServerMemoryOptimizedTablesConventionTest.cs` | VistaDB does not support memory-optimized tables. |
| `Metadata/Conventions/SqlServerOutputClauseConventionTest.cs` | VistaDB does not support the SQL `OUTPUT` clause. |
| `SqlServerNTSApiConsistencyTest.cs` | NTS/spatial is an opt-in NuGet (`EFCore.SqlServer.NTS`); there is no VistaDB equivalent. |
| `Storage/Internal/SqlServerGeometryTypeMappingTests.cs` | Geometry mapping is NTS-only; VistaDB has no spatial. |
| `Storage/SqlServerRetryingExecutionStrategyTests.cs` | VistaDB is file-based — no transient errors, so no retry strategy. |
| `ValueGeneration/SqlServerSequenceValueGeneratorTest.cs` | VistaDB has no sequences (no `CREATE SEQUENCE`); HiLo/sequence value generators do not apply. |

## VistaDB feature gaps applied to ported files

When porting the remaining 30 SqlServer tests, the following SqlServer-only scenarios
are dropped (and preserved verbatim in a `VistaDB: no analog —` marker block for future revival):

- Schemas (non-default schemas like `dbo.other`)
- Sequences (CREATE SEQUENCE, HiLo)
- Computed columns (`HasComputedColumnSql`)
- Temporal / system-versioned tables (`IsTemporal`)
- Memory-optimized tables (`IsMemoryOptimized`)
- `MERGE` statement and the `OUTPUT` clause
- Vector / UDT / `sql_variant` / structural JSON type mappings
- Azure SQL / Synapse engine-type tests
- `AT TIME ZONE`, `DATEDIFF_BIG`, `DATEFROMPARTS`
- `sp_addextendedproperty` / extended properties
- Full-text search (`CONTAINS` / `FREETEXT`)
- Stored procedures
- Transient-error retry strategy
