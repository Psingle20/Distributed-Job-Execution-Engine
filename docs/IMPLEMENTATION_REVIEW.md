# Distributed Job Execution Engine - Full Implementation Review

**Review date:** 2026-09-08  
**Scope:** JobService, WorkerService, shared building blocks, EF Core outbox package, contracts, Dapr components, local infrastructure, observability, UI, documentation, and tests.

## Executive summary

The project has a strong architecture and a compelling community-demo story. The service ownership is clear, the job lifecycle belongs to JobService, WorkerService reports outcomes through its own transactional outbox, and the repository contains unusually good supporting material: ADRs, migrations, failure-injection controls, observability configuration, architecture tests, and a demo UI.

The implementation is not yet demo-ready or production-grade in runtime behavior. Several release-blocking defects currently prevent the documented end-to-end guarantees from holding:

1. JobService does not register `DaprClient`, although its outbox dispatcher requires it.
2. The outbox PostgreSQL migration and claim strategy use incompatible identifier conventions and the migration contains a SQL Server-style index filter.
3. retry events are published immediately but acknowledged when the future retry cannot yet be claimed, permanently losing the retry trigger.
4. Running executions are not committed before the handler runs, so the heartbeat service cannot discover them.
5. Outcome events are not fenced by attempt or lease token, allowing stale workers to affect a later attempt.
6. Docker Compose starts only infrastructure even though the README and demo script say it starts both services, sidecars, and three workers.

**Readiness assessment:**

| Area | Assessment | Notes |
|---|---|---|
| Architecture and service boundaries | Strong | Good DDD ownership and clean layering |
| Domain model and state machine | Strong foundation | Correct central ownership; distributed fencing is incomplete |
| Outbox package design | Promising | Good API and tests; PostgreSQL integration is currently broken |
| Distributed reliability | Not ready | Retry, heartbeat, stale-outcome, and deduplication defects |
| Observability | Scaffolded | Metrics are defined but never recorded; outbox spans are not subscribed |
| Test strategy | Partial | Domain/package tests are useful; critical integration and race tests are absent |
| Local demo | Not ready | Compose and browser-control paths do not match the documentation |
| Production hardening | Not ready | Authentication, authorization, secrets, and real health checks are absent |

## What is working well

- Each service follows `Api -> Application -> Domain`, with infrastructure depending inward through interfaces.
- JobService is the single owner of canonical job state and the state machine.
- WorkerService owns local execution records and uses its own outbox for completion/failure events.
- PostgreSQL `xmin` is configured as an optimistic concurrency token on `Job`.
- The domain tests cover state transitions, lease ownership, retry limits, cancellation, and terminal behavior.
- The outbox package includes locking, retries, dead-letter state, retention, health-check support, CloudEvent metadata, and focused SQLite tests.
- The project has a useful set of ADRs and an effective demo-oriented UI concept.
- Warnings are treated as errors and architecture boundaries are enforced with tests.

## Release-blocking findings

### P0-1: JobService cannot construct the outbox dispatcher

**Evidence:** `JobService.Api/Program.cs:26-47` configures controllers, telemetry, and dependency modules but never calls `AddDaprClient()`. `PollingOutboxDispatcher.cs:76-81` requires a `DaprClient` constructor dependency. WorkerService registers it at `WorkerService.Api/Program.cs:45`, confirming the intended wiring.

**Impact:** JobService startup or hosted-service activation fails when DI resolves the dispatcher. No `jobs.created` event can be published.

**Required change:** Register `builder.Services.AddDaprClient()` in JobService before building the app. Add a service-provider validation/startup test that resolves every hosted service.

### P0-2: The PostgreSQL outbox schema and claim SQL are incompatible

**Evidence:** Both services call `UseSnakeCaseNamingConvention()`. The migration creates columns such as `id`, `processed_at`, and `attempt_count` (`JobService` migration lines 18-31), while `PostgreSqlOutboxClaimStrategy.cs:58-72` queries quoted identifiers such as `"Id"`, `"ProcessedAt"`, and `"AttemptCount"`. Quoted PostgreSQL identifiers are case-sensitive.

The generated migration also uses `filter: "[ProcessedAt] IS NULL"` at `20260905112852_InitialCreate.cs:123-127`. Square-bracket identifier syntax is SQL Server-specific and the property name does not match the snake-case column.

**Impact:** The initial PostgreSQL migration is expected to fail while creating the filtered index. If that is corrected alone, the specialized PostgreSQL claim strategy still fails at runtime because its SQL references columns that do not exist.

**Required change:** Make index filters provider-aware and resolve every table/column identifier from EF Core relational metadata (`StoreObjectIdentifier` plus `GetColumnName`), then quote it with the provider's SQL generation helper. Add real PostgreSQL Testcontainers tests for migration, concurrent claims, release, retry, and dead-letter behavior. Regenerate both service migrations afterward.

### P0-3: Scheduled retries lose their only wake-up event

**Evidence:** `Job.Fail` calculates a future `retryAt` and `HandleJobExecutionFailedCommandHandler.cs:34-37` immediately enqueues `jobs.retry-scheduled`. WorkerService receives it immediately and calls `ClaimJobAsync` (`HandleJobRetryScheduledCommandHandler.cs:33`). `Job.CanBeClaimed` rejects the claim until `NextRunAt`, but the worker converts any claim failure into `Result.Success()` at lines 34-36. The controller then returns HTTP 200 and Dapr acknowledges the message.

**Impact:** Automatic retries normally remain in `Retrying` forever. Lease recovery has the same failure because it publishes the same future-dated event immediately.

**Required change:** Introduce a durable scheduler. A simple, production-suitable option is a JobService background dispatcher that atomically selects due `Retrying` jobs and emits a ready-to-run event only after `NextRunAt`. Alternative Dapr scheduling mechanisms are acceptable if delivery is durable and testable. Do not hold a pub/sub HTTP request open until `RetryAt`.

### P0-4: Heartbeats cannot see active executions

**Evidence:** `HandleJobCreatedCommandHandler.cs:39-47` adds a `WorkerExecution` to the DbContext and starts the handler, but `SaveChangesAsync` is not called until lines 61-62 after execution finishes. The retry handler follows the same pattern. `WorkerHeartbeatHostedService.cs:54-55` uses a separate scope/DbContext and queries persisted `Running` rows.

**Impact:** A job running longer than the 30-second lease emits no heartbeat. JobService recovers its lease and schedules another attempt while the original handler is still running, creating concurrent side effects and stale outcome events.

**Required change:** Persist the claimed execution before starting work, or maintain a process-local active-execution registry that the heartbeat service owns. Because claim and local persistence cross service/database boundaries, explicitly define crash recovery for the interval between them. Prefer an execution coordinator/background queue over executing the job inside the Dapr subscription request.

### P0-5: Outcomes are not fenced to the active attempt

**Evidence:** completion and failure contracts contain `AttemptNumber`, but `HandleJobExecutionCompletedCommandHandler.cs:26` calls `job.Complete(command.WorkerId, ...)` and the failure handler similarly calls `job.Fail(...)`. `Job.Complete` and `Job.Fail` validate only `State` and `WorkerId` (`Job.cs:118-150`). The claim response returns no execution ID or lease token.

**Impact:** A delayed outcome from an expired execution can complete or fail a later attempt when the worker identity is reused. This is especially likely because worker identity is process-wide and retries can return to the same process. Optimistic concurrency prevents simultaneous writes; it does not establish which execution is authoritative.

**Required change:** Generate an opaque `ExecutionId` or monotonically increasing fencing token during claim and return it to WorkerService. Include it in heartbeats and outcome events. JobService must accept mutations only when `State == Running`, `WorkerId`, `AttemptNumber`, and `ExecutionId` all match the active lease.

### P0-6: The documented full-stack command does not start the applications

**Evidence:** `deploy/compose/docker-compose.yml` defines PostgreSQL, Redis, OTel Collector, Tempo, Prometheus, and Grafana only. It has no JobService, WorkerService, or Dapr sidecar services. `README.md:129-141` and `docs/demo-script.md:6-14` claim the same command starts JobService and three worker replicas.

**Impact:** Following the demo instructions leaves ports 5000 and 5101-5103 closed. The control panel and all job scenarios are unavailable.

**Required change:** Either add the applications and sidecars to Compose with health-conditioned dependencies, per-instance Dapr ports, connection strings, and OTLP endpoints, or update the instructions to start infrastructure plus the existing VS Code/Dapr tasks. For a community call, one deterministic command is strongly preferable.

## High-priority correctness findings

### P1-1: Deduplication uses a trace identifier instead of the message identifier

All Dapr subscription actions derive `messageId` from `Traceparent` and fall back to a new GUID, for example `WorkerService.Api/DaprSubscriptionController.cs:18-24` and `JobService.Api/DaprSubscriptionController.cs:17-25`. A trace header is observability context, not a stable delivery identity. A redelivery can receive a different span context, while separate events can also participate in one trace.

Use the CloudEvent `id` generated from the outbox message ID. Bind a CloudEvent envelope or reliably extract the CloudEvent ID supplied by Dapr, and test that the same event delivered twice creates one processed-message row and one side effect.

### P1-2: Retry attempt numbers are off by one

JobService publishes the current completed attempt count in `JobRetryScheduledIntegrationEvent`. WorkerService then claims the job, which increments the canonical attempt, but creates its execution and outcome using the event's old attempt number (`HandleJobRetryScheduledCommandHandler.cs:33-59`). This conflicts with the unique `(JobId, AttemptNumber)` index and makes telemetry and fencing incorrect.

The claim response should be authoritative and return the newly allocated attempt number plus execution token. WorkerService must not infer the attempt number from the scheduling event.

### P1-3: Cancellation is neither persisted nor applied to running work

`HandleJobCancelledCommandHandler.cs:7-19` only adds a processed-message entity. It has no unit of work and never calls `SaveChangesAsync`, so even the deduplication marker is discarded. It also does not signal or cancel an active handler.

Add an execution coordinator keyed by `ExecutionId`, cancel its token when a valid cancellation arrives, persist the message marker, and define how non-cooperative handlers are fenced from reporting success after cancellation.

### P1-4: Unknown job types are acknowledged and left pending forever

Both WorkerService handlers return success when no handler is registered (`HandleJobCreatedCommandHandler.cs:27-30` and retry equivalent). The event is acknowledged, no outcome is emitted, and the Job remains `Pending` or `Retrying` without a lease to recover.

Validate supported job types at JobService creation, or publish a non-retryable execution failure for an unsupported type. Prefer both defensive checks.

### P1-5: Failure injection does not match the labeled scenarios

- `duplicate-event` is activated by the controller but is never consumed anywhere, so the button has no effect.
- `crash-after-claim` and `crash-after-effect` are both checked after the inner command handler has completed and committed (`ChaosCommandDecorator.cs:20-24, 78-95`). They are behaviorally almost identical and neither crashes the process after claim.
- Throwing `ChaosException` simulates a failed request, not a worker process crash. The distinction matters when demonstrating lease recovery.

Implement explicit hook points in an execution coordinator: after remote claim, after local execution record commit, after side effect, after outcome commit, and before response. For the crash scenario, terminate only the selected worker container/process and let orchestration restart it.

### P1-6: The retry demo handler is process-local and unreliable across replicas

`FailNTimesThenSucceedHandler.cs:11-37` counts failures in a static in-memory dictionary. Attempts distributed across three replicas do not share the count, and a restart resets it. With the current retry attempt bug it also receives the same attempt number repeatedly.

For a deterministic demo, fail based on the authoritative attempt number (`attempt <= requestedFailureCount`). This remains stateless and behaves consistently across worker replicas and restarts.

### P1-7: The existing application tests are excluded and do not compile

`DistributedJobEngine.Dev.slnx:29-31` includes architecture and domain tests but omits `JobService.Application.Tests`. Running that project directly produces five `CS7036` errors because handler constructor dependencies changed without updating tests. `JobService.slnx` also includes only the domain test project, and WorkerService has no application/domain behavior test project.

Add every test project to the root and service solutions, repair the stale mocks, and make the root CI command discover them all.

## Medium-priority findings

### P2-1: Observability is declared but not emitted end to end

`JobServiceDiagnostics` and `WorkerServiceDiagnostics` define counters, but there are no `.Add(...)` calls anywhere in the services. The dashboard queries those counters, so its business panels remain empty. The outbox creates spans under `Dapr.EntityFrameworkCore.Outbox`, but neither service registers that activity source in `Program.cs`.

Record metrics at successful state transitions, claims, duplicates, outcomes, and heartbeats. Register the outbox activity source and add trace tags for `job.id`, `execution.id`, `attempt`, `worker.id`, `message.id`, and topic. Add outbox lag/pending/dead-letter metrics.

### P2-2: Browser controls for WorkerService require CORS

The UI is served from port 5000 and directly calls `http://localhost:5101` (`index.html:172`), but WorkerService configures no CORS policy or middleware. Browser requests to WorkerService chaos/status endpoints will be blocked.

Proxy demo control requests through JobService or add a tightly scoped development-only CORS policy for the JobService origin.

### P2-3: Health endpoints always report healthy

Both `/health` endpoints return a constant object and do not check PostgreSQL, Dapr sidecar availability, outbox backlog, or worker readiness. The package's outbox health check is implemented but unused.

Add separate liveness/readiness checks. Readiness should include database connectivity and bounded outbox lag; decide whether sidecar health is a readiness dependency based on desired backpressure.

### P2-4: Security posture is appropriate only for a local demo

Public job mutation, internal claim/heartbeat routes, and chaos controls have no authentication or authorization. PostgreSQL and Grafana credentials are hard-coded, and Grafana grants anonymous Admin access. This is acceptable only under a clearly documented local-demo profile.

Add service-to-service access control, user/API authorization, secrets injection, payload size limits, and a configuration gate that completely disables chaos endpoints outside demo mode before describing deployment as production-ready.

### P2-5: Result payload is discarded by canonical JobService state

The worker includes `ResultPayload` in the completion event and command, but the completion handler does not store it. The JobService `JobExecution` model and `JobResponse` therefore cannot show the artifact/output of report or export jobs.

Persist a bounded result descriptor, preferably an artifact URI plus content metadata rather than a large result body. Keep large artifacts in object storage.

### P2-6: Retryability in the contract is ignored

`JobExecutionFailedIntegrationEvent` contains `IsRetryable`, but the JobService failure handler always calls `Job.Fail`, which schedules retries solely from attempt count. Either honor the flag in the state machine or remove it from the contract until supported.

### P2-7: Domain-event integration between shared packages is disconnected

The outbox interceptor discovers `Dapr.EntityFrameworkCore.Outbox.IHasDomainEvents`, while `CleanArchitecture.BuildingBlocks.Entity` does not implement that interface and exposes `List<IDomainEvent>`. The services work around this by explicitly enqueuing integration events, but README statements that aggregate domain events are intercepted do not describe this implementation.

Keep explicit domain-to-integration mapping as the preferred service pattern, or add a neutral abstraction shared without forcing the domain package to depend on Dapr. Document the chosen model precisely.

### P2-8: Documentation contains broken or inaccurate references

The README ADR links use names such as `docs/adr/001-clean-architecture-per-service.md`, while actual files are prefixed and named differently (for example `ADR-001-use-clean-architecture-per-service.md`). It also describes an advisory-lock claim strategy, although the implementation uses row locks with `FOR UPDATE SKIP LOCKED`.

Repair links and align operational claims with code after the runtime fixes are complete.

## Verification performed

### Root solution

Command:

```powershell
dotnet test DistributedJobEngine.Dev.slnx --no-restore
```

Result: **passed**, exit code 0.

- CleanArchitecture.BuildingBlocks.Tests: 9 passed
- Dapr.EntityFrameworkCore.Outbox.Tests: 28 passed
- JobService.Domain.Tests: 51 passed
- JobService.ArchitectureTests: 8 passed
- WorkerService.ArchitectureTests: 9 passed
- Total discovered by root solution: 105 passed

### Omitted application tests

Command:

```powershell
dotnet test services/JobService/tests/JobService.Application.Tests/JobService.Application.Tests.csproj --no-restore
```

Result: **failed to compile** with five missing-constructor-argument errors in create, cancel, retry, lease-recovery, and failed-outcome handler tests.

### Compose validation

`docker compose config --quiet` parsed the Compose file successfully, with a local Docker config access warning. This validates YAML shape only; containers and PostgreSQL migrations were not started during this review.

## Missing tests required for the reliability claims

1. PostgreSQL migration succeeds from an empty database for both services.
2. Two outbox dispatchers claim disjoint rows under concurrent load.
3. A publish succeeds but the process dies before marking the row; redelivery has one business effect.
4. A failed job becomes runnable only at `NextRunAt` and is eventually dispatched without another external event.
5. A 60-second job persists its active execution and renews a 30-second lease.
6. A worker dies after claim; the lease expires and exactly one later attempt is accepted.
7. A stale completion/failure from attempt N is rejected after attempt N+1 is active.
8. Duplicate CloudEvent delivery produces one execution/outcome.
9. Two worker replicas race to claim one job; exactly one executes it.
10. Cancelling a running job signals the handler and rejects any later success event.
11. Unsupported job type reaches a clear terminal failure rather than remaining pending.
12. Full Compose smoke test creates a job, observes it complete, and finds its trace and metrics.

## Recommended repair order

### Phase 1: Make one happy path executable

1. Register JobService `DaprClient`.
2. Fix provider-aware outbox schema/index/claim SQL and regenerate migrations.
3. Add applications and Dapr sidecars to Compose.
4. Add a PostgreSQL + Dapr smoke test for create -> publish -> claim -> complete.

### Phase 2: Make retries and recovery correct

1. Add durable due-job scheduling.
2. Return `ExecutionId`, attempt number, and lease expiry from claim.
3. Persist active executions before handler execution.
4. Fence heartbeat and outcome operations.
5. Add stale-worker and crash-recovery integration tests.

### Phase 3: Make the demo truthful and observable

1. Wire metrics and outbox tracing.
2. Fix CORS/proxying and all chaos hook points.
3. Make `FailNTimesThenSucceed` attempt-based.
4. Store result artifact descriptors.
5. Run every scripted scenario from a clean machine/volume.

### Phase 4: Production hardening

1. Add authentication, authorization, service access policy, and secret management.
2. Add readiness/liveness and operational outbox controls.
3. Bound concurrency, payload sizes, execution duration, and graceful shutdown.
4. Add CI for format, build, all tests, PostgreSQL integration, container build, and Compose smoke test.

## Final assessment

This is a worthwhile and technically substantial project. Its architecture already communicates the right ideas to the Dapr community: transactional state plus outbox, at-least-once delivery, idempotent consumers, lease-based recovery, and clear domain ownership. The main task now is to make the executable behavior match those claims.

After the P0 items and the reliability integration tests are complete, the project should be suitable for a strong community demo. After P1/P2 hardening, it can credibly be described as a production-oriented reference implementation; until then, call it a production-grade architecture under active implementation rather than a production-grade engine.
