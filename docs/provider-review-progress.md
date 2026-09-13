# VistaDB provider review progress

Implementation was authorized after the original review. No commits before user review. Existing test sources, assertions, skips and expected output remain unchanged.

Baseline: EF Core 10.0.8 / VistaDB 6.6.2 / net10.0, commit `0ca0eeafbacbbd424453e51b803a5a751225a766`.

The user's migration and external `.vdb6` editor failures motivated the multiprocess default. Preserve that behavior. Reverting to single-process to make tests pass is not an acceptable resolution.

| Item | Current disposition | Evidence and remaining gate |
|---|---|---|
| D01 Nullable unique indexes | Retain workaround; clarify lost enforcement | VDB rejects tested filtered-index replacement and duplicate NULLs in a unique index. Non-null uniqueness is also lost; EF tracking does not replace it. |
| D02 Alternate-key foreign keys | Retain native limitation; analyze jointly with D04 | Tested native FK target requires primary key. Suppressed alternate-key FKs do not protect external writers. |
| D03 Identifier sanitization | Candidate normalization alignment and collision validation retained | Actual DDA rename of spaced names and table collision rejection pass. Broader index/constraint collisions remain. |
| D04 Identity transaction ownership | SQL replacement rejected and removed; original DDA path retained | Full suite found 20 regressions missed by five original focused reproductions. All 20 pass after restoration. DDA rollback/isolation defect remains unresolved. |
| D05 DDA generated-value readback | Candidate retained | Actual plain and converted defaults read back successfully; prior full functional snapshots cover this correction. Timestamp coverage remains. |
| D06 Concurrency classification | Candidate mapped relational probes retained | Converted keys, NULL conditions and async stale tokens verified. EF concurrency-event interception/suppression remains a separate open contract. |
| D07 Identity metadata | Candidate retained | Actual model/database identity seed 100 and increment 5 produce 100,105. Broader shared/TPT metadata remains. |
| D08 Column alteration | Candidate retained | DDA length, default removal and escaped string default verified. Broader facet/comment preservation remains. |
| D09 History and scripts | Candidate retained | Documented catalog and conditional SQL, repeated bootstrap, actual migration/revert pass. DDA-only script export rejects explicitly. |
| D10 Migration coordination | Candidate retained | Separate-process exclusion, cancellation, process-death recovery and actual SQL/DDA migration/revert pass. Sidecar locks coordinate EF migrators, not editor operations. |
| D11 Migration operation mutation | Candidate retained | Caller operations preserved across repeat generation; forward FK before table rename verified against VDB. Complex rename/drop sequences remain. |
| D12 Lifecycle/scaffolding | Partial candidate retained | Encryption, targeted creator deletion, unrelated ADO/DDA survival and Unicode scaffold length verified. Migration-level global cleanup and other metadata fidelity remain. |
| D13 Translation/type semantics | Narrow candidate fixes retained | Wildcard char, empty IndexOf with start, and submillisecond literal checks pass. Broader temporal/rowversion/binary semantics remain. |
| D14 Connection contracts | Candidate empty-string/ambient corrections retained; multiprocess default preserved | All 36 connection-interception cases pass. 39 baseline mixed-family failures unresolved. External process coexistence with DDA migrations verified. |

## Validation record

- Baseline unit: 690 passed, 11 skipped. Baseline functional: 15,347 passed, 55 failed, 117 skipped. Failures: 39 VDB 219 open-mode conflicts and 16 connection-interception mismatches.
- Functional iterations 2 and 3: 15,363 passed, 39 failed, 117 skipped. All remaining failures are baseline VDB 219 conflicts.
- Rejected D04 SQL candidate: five original identity/graph reproductions passed, as did explicit identity user rollback, failed-batch rollback and async rollback probes. **Full functional iteration 4 failed acceptance: 15,343 passed, 59 failed, 117 skipped.** It added 20 ClientNoAction alternate-key failures where no DbUpdateException was thrown.
- Restoring only the prior modification-batch implementation (including retained D05/D06 fixes) makes all 20 affected unchanged cases pass. The rejected source is preserved externally as `D04-rejected-sql-identity-candidate.cs`, not active in the repository.
- Latest unit iteration 7: 690 passed, 11 skipped, zero failures. An intermediate rebuild collided with the running functional test's locked DLL; the subsequent sequential rebuild/run succeeded.
- No fresh whole-suite run has been performed on the final restored combination. The 20 affected cases were rerun; the original DDA path was covered by full iterations 2/3, while D09/D10 were covered by iteration 4. Do not present these as a single fully green final run.
- Test source/configuration SHA-256 comparison against baseline: zero differences. Supplemental executables are external to the existing suite.

Passing totals are not complete upstream compliance: eight imported upstream differences, existing skips/no-op overrides, and uninstantiated upstream fixture families remain documented in the original review.

## D04 / D02 interaction and next plan

The failed unchanged cases delete an existing dependent inside an ADO transaction, mark it Added again, then delete its principal with client cascade disabled. The original independent DDA insert does not share the ADO transaction's view.

A separate native/provider isolation probe confirms that after an ADO transaction deletes a row, its SQL query sees zero rows while an independent DDA handle still sees one. DDA reinsertion of that key fails with engine 309 duplicate-key even without any FK on the table. This establishes a mechanism by which an expected DbUpdateException can occur for the wrong reason. It is consistent with the 20-case behavior; it is not proof of complete referential enforcement.

The same-connection SQL candidate fixes the measured rollback problem but cannot be accepted while it permits those required scenarios to succeed. Address native alternate-key enforcement limits and transaction ownership together. Do not add an arbitrary throw or weaken assertions merely to regain passes. The next design must demonstrate correct rejection reasons, rollback, original graph behavior and external-writer semantics against VDB, then pass the unchanged suite.

## Multiprocess/editor acceptance

VDB's raw ADO default is SingleProcessReadWrite; this provider defaults string-created connections to MultiProcessReadWrite. The engine rejects mixing these families. The 39 failures remain a visible acceptance exception. Silently changing caller-supplied connections or downgrading the provider default is not an accepted fix.

A separate process held a MultiProcessReadWrite ADO connection open while the actual EF migrator reverted and reapplied a DDA alteration. This passed. It tests external connection coexistence, not every operation of the GUI `.vdb6` editor. Sidecar migration locking cannot override VDB schema/table locks, and transaction-suppressed DDA migrations are not made atomic by that lock.

## .NET 10 / C# 14

The code already targets .NET 10 and was exercised with SDK 10.0.401 and EF Core 10.0.8. Correct async cancellation, disposal, parameter mapping and truthful capability flags take precedence over cosmetic C# 14 rewrites. Further type/collection modernization must preserve VDB semantics and demonstrate a concrete benefit.

No commits have been made. Open items above remain work to resolve or explicitly review, not completed fixes.

## Local validation package

At the user's request, the retained candidate was cleaned and packed in Release as Microsoft.EntityFrameworkCore.VistaDB 1.0.10, then published to the configured Personal (Local) feed at D:\Development\NuGet. The published package's SHA-256 matches the built artifact. No commit was made.

A separate consumer restored the actual nupkg into an isolated package cache and verified assembly version 1.0.10.0, the multiprocess default, database creation, explicit identity/default readback through the retained DDA path, a wildcard-character query, and database deletion. This packaging check does not supersede the full-suite limitations above. Build succeeded with the five already identified nullable-reader/obsolete-VDB-attribute warnings.

## Publication approval

The user subsequently authorized committing the 1.0.10 candidate and publishing it to both configured Git remotes on their main branches. This supersedes the earlier no-commit hold; the validation results and unresolved limitations above remain unchanged.
