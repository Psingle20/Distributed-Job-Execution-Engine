# Distributed Job Execution Engine — Implementation Checklist

Target: September 30, 2026 Community Demo
Status legend: `[ ]` todo, `[x]` done, `[-]` skipped/deferred

---

## M1: Project Structure

- [x] Create folder structure: `packages/`, `services/`, `deploy/`, `docs/`
- [x] Create `DistributedJobEngine.Dev.sln` (root development solution)
- [x] Create `Directory.Build.props` (net10.0, warnings as errors, analyzers)
- [x] Create `Directory.Packages.props` (central package management)
- [x] Create `.editorconfig`
- [x] Create `.gitignore`
- [x] Initialize git repository
- [x] Write ADR-001: Use Clean Architecture per service
- [x] Write ADR-002: Keep job state machine inside JobService.Domain
- [x] Write ADR-003: Use standalone EF Core outbox package
- [x] Write ADR-004: Use Docker Compose for local demo (MiniStack later)
- [x] Write ADR-005: Use at-least-once delivery with idempotent consumers
- [x] Write ADR-006: Use worker leases instead of distributed locks
- [x] Write ADR-007: Make contracts package optional for template
- [x] Write ADR-008: Use chaos middleware for failure injection

---

## M2: Extract Dapr.EntityFrameworkCore.Outbox

Source: `dotnet-sdk` repo, branch `backup/ef-core-outbox-full`

- [x] Create `packages/Dapr.EntityFrameworkCore.Outbox/`
- [x] Create `Dapr.EntityFrameworkCore.Outbox.sln`
- [x] Copy `src/Dapr.EntityFrameworkCore.Outbox/` from backup branch
- [x] Copy `test/Dapr.EntityFrameworkCore.Outbox.Test/` from backup branch
- [x] Adjust namespaces if needed
- [x] Adjust .csproj — target net10.0, update package versions
- [x] Verify `IHasDomainEvents` interface is generic (no business concepts)
- [x] Verify `OutboxMessage` entity
- [x] Verify `DaprOutboxSaveChangesInterceptor`
- [x] Verify `PollingOutboxDispatcher`
- [x] Verify `PostgreSqlOutboxClaimStrategy`
- [x] Verify `RelationalOutboxClaimStrategy` (fallback)
- [x] Verify `DaprOutboxHealthCheck`
- [x] Verify `OutboxRetentionHostedService`
- [x] Verify `DaprOutboxDiagnostics` (OpenTelemetry ActivitySource)
- [x] Verify DI extensions (`AddDaprOutbox<TDbContext>`)
- [x] Verify `ModelBuilderExtensions.AddDaprOutbox()`
- [x] Verify `DbContextExtensions.EnqueueOutbox()`
- [x] Run unit tests — all 28 pass
- [x] Add to root dev solution
- [x] Add README with usage example

---

## M3: Extract CleanArchitecture.BuildingBlocks

Source: `cleanarchitecure` repo, `Service.SharedKernel` + `Service.Application` abstractions

- [x] Create `packages/CleanArchitecture.BuildingBlocks/`
- [x] Create `CleanArchitecture.BuildingBlocks.slnx`
- [x] Create `src/CleanArchitecture.BuildingBlocks/` project
- [x] Copy from SharedKernel: `Entity`, `IDomainEvent`, `IDomainEventHandler<T>`
- [x] Copy from SharedKernel: `Result`, `Result<T>`, `Error`, `ErrorType`, `ValidationError`
- [x] Copy from SharedKernel: `IDateTimeProvider`, `IDependencyModule`
- [-] Have `Entity` implement outbox package's `IHasDomainEvents` interface (deferred — each service bridges via AggregateRoot)
- [x] Copy CQRS abstractions: `ICommand`, `ICommand<T>`, `IQuery<T>`
- [x] Copy CQRS abstractions: `ICommandHandler`, `IQueryHandler`
- [x] Copy dispatchers: `ICommandDispatcher`, `CommandDispatcher`
- [x] Copy dispatchers: `IQueryDispatcher`, `QueryDispatcher`
- [x] Copy processor abstractions: `IProcessor`, `IDto`, `IRequestHandler`, `RequestHandler`
- [x] Copy decorators: `LoggingDecorator`, `ValidationDecorator`
- [x] Add DI registration: `AddBuildingBlocks()`, `AddLoggingDecorator()`, `AddValidationDecorator()`
- [-] Add pagination primitives if needed (deferred to M5)
- [x] Create `tests/CleanArchitecture.BuildingBlocks.Tests/`
- [x] Write tests for `Result`, `Error`, dispatchers, validation decorator — 9 pass
- [x] Strip any Todo/User domain references
- [x] Verify no business-specific concepts leaked in
- [x] Run tests — all 9 pass
- [x] Add to root dev solution

---

## M4: JobEngine.Contracts

- [x] Create `packages/JobEngine.Contracts/`
- [x] Create project `JobEngine.Contracts.csproj`
- [x] Define `JobCreatedIntegrationEvent`
- [x] Define `JobRetryScheduledIntegrationEvent`
- [x] Define `JobCancelledIntegrationEvent`
- [x] Define `JobExecutionCompletedIntegrationEvent`
- [x] Define `JobExecutionFailedIntegrationEvent`
- [x] Define `JobCompletedIntegrationEvent`
- [x] Define `JobFailedIntegrationEvent`
- [x] Add to root dev solution

---

## M5: JobService

### M5.1: Solution Scaffold

- [x] Create `services/JobService/JobService.slnx`
- [x] Create `JobService.Api` project
- [x] Create `JobService.Application` project
- [x] Create `JobService.Domain` project
- [x] Create `JobService.Infrastructure` project
- [x] Add ProjectReferences to BuildingBlocks, Outbox, Contracts
- [x] Wire up DI composition (`IDependencyModule` per layer)
- [x] Add to root dev solution

### M5.2: Domain — Job Aggregate & State Machine

- [x] Create `JobState` enum (Pending, Running, Retrying, Completed, Failed, Cancelled)
- [x] Create `Job` aggregate root with all properties from doc
- [x] Implement `Job.Create(...)` factory method
- [x] Implement `Job.Claim(workerId, now, leaseDuration)`
- [x] Implement `Job.Heartbeat(workerId, now, leaseDuration)`
- [x] Implement `Job.Complete(workerId, now)`
- [x] Implement `Job.Fail(workerId, now, error)`
- [x] Implement `Job.ScheduleRetry(workerId, now, retryAt, error)`
- [x] Implement `Job.Cancel(now)`
- [x] Implement `Job.MarkLeaseExpired(now, retryAt)`
- [x] Implement `Job.CanBeClaimed(now)`
- [x] Create `JobStateMachine` — validate all allowed transitions
- [x] Create `JobRetryPolicy` — exponential backoff with jitter
- [-] Create `JobLease` value object (inlined as properties on Job aggregate)
- [x] Create `JobStateTransition` entity
- [-] Create `JobType` value object (using string — simpler for demo)
- [x] Create `JobErrors` static error definitions
- [x] Domain events: `JobCreatedDomainEvent`, `JobClaimedDomainEvent`, etc. (7 events)
- [x] Create `JobExecution` entity
- [x] Optimistic concurrency via `Version` property (uint / xmin)
- [x] `RetryFromFailed()` — manual retry from Failed state

### M5.3: Domain Tests

- [x] Test all valid state transitions (10 allowed transitions)
- [x] Test all rejected state transitions (8 disallowed transitions)
- [x] Test claim rules (only Pending/Retrying, only below max attempts)
- [x] Test heartbeat rules (only current lease owner)
- [x] Test complete/fail rules (only current lease owner, only Running)
- [x] Test lease expiry logic (retry and terminal fail paths)
- [x] Test retry policy backoff calculations (4 tests)
- [x] Test cancel from allowed states (Pending, Running)
- [x] Test terminal states (Completed, Cancelled cannot transition)
- [x] All 51 tests pass

### M5.4: Application — Commands & Queries

- [x] `CreateJobCommand` + handler + validator
- [x] `CancelJobCommand` + handler
- [x] `RetryJobCommand` + handler (Failed → Retrying)
- [x] `ClaimJobCommand` + handler (internal, for workers)
- [x] `HeartbeatJobCommand` + handler (internal)
- [-] `ReleaseJobCommand` + handler (deferred — covered by lease expiry)
- [x] `HandleJobExecutionCompletedCommand` + handler (consumes worker outcome, idempotent)
- [x] `HandleJobExecutionFailedCommand` + handler (consumes worker outcome, idempotent)
- [x] `RecoverExpiredLeasesCommand` + handler (background)
- [x] `GetJobByIdQuery` + handler
- [x] `SearchJobsQuery` + handler (with pagination)
- [-] `GetJobTimelineQuery` + handler (deferred — transitions available via job aggregate)
- [-] `GetJobExecutionsQuery` + handler (deferred — executions available via job aggregate)
- [x] `GetJobSummaryQuery` + handler (counts by state)
- [x] Define `IJobRepository` (with search, expired leases, summary counts)
- [-] Define `IJobTimelineRepository` (merged into IJobRepository)
- [x] Define `IProcessedMessageRepository`
- [x] Define `IUnitOfWork`
- [-] Define `IOutboxEventWriter` (deferred — outbox writes handled by interceptor)
- [-] Processors for each public DTO (deferred to API endpoints)
- [x] Validators (FluentValidation) for CreateJobCommand

### M5.5: Infrastructure — Persistence

- [x] Create `JobDbContext`
- [x] EF configuration for `Job` (table: `jobs`, all columns from data model)
- [x] EF configuration for `JobExecution` (table: `job_executions`)
- [x] EF configuration for `JobStateTransition` (table: `job_state_transitions`)
- [x] EF configuration for `ProcessedMessage` (table: `processed_messages`)
- [x] Map outbox table via `modelBuilder.AddDaprOutbox()`
- [x] Attach `DaprOutboxSaveChangesInterceptor` (via `AddDaprOutbox<JobDbContext>()`)
- [x] Configure optimistic concurrency token (`xmin` via `.IsRowVersion()`)
- [x] Create indexes from data model (4 indexes on jobs, 2 on executions, 1 on transitions)
- [x] Create initial migration (InitialCreate)
- [x] Implement `JobRepository` (with search, expired leases, summary counts)
- [-] Implement `JobTimelineRepository` (merged into JobRepository)
- [x] Implement `ProcessedMessageRepository`
- [-] Implement `EfCoreOutboxEventWriter` (outbox handled by interceptor)
- [x] Register `AddDaprOutbox<JobDbContext>()` with Postgres claim strategy

### M5.6: Infrastructure — Background Services

- [x] `LeaseRecoveryBackgroundService` — polls expired leases every 5s
- [x] Wire lease recovery to `RecoverExpiredLeasesCommand` via `ICommandDispatcher`

### M5.7: Api — Endpoints

- [x] `POST /jobs` — create job
- [x] `GET /jobs/{jobId}` — get job by id
- [x] `GET /jobs` — search/list jobs (with state/type filters, pagination)
- [x] `POST /jobs/{jobId}/cancel` — cancel job
- [x] `POST /jobs/{jobId}/retry` — manual retry
- [-] `GET /jobs/{jobId}/timeline` — deferred (data available via aggregate)
- [-] `GET /jobs/{jobId}/executions` — deferred (data available via aggregate)
- [x] `GET /jobs/summary` — counts by state
- [x] `POST /internal/jobs/{jobId}/claim` — worker claim endpoint
- [x] `POST /internal/jobs/{jobId}/heartbeat` — worker heartbeat
- [-] `POST /internal/jobs/{jobId}/release` — deferred (covered by lease expiry)
- [x] Dapr subscription endpoint for `job-executions.completed`
- [x] Dapr subscription endpoint for `job-executions.failed`
- [x] Health check endpoint

### M5.8: Architecture Tests

- [x] Layer dependency tests (Domain, Application, Infrastructure, Api)
- [x] Domain must not reference EF Core, Dapr, or Infrastructure

---

## M6: WorkerService

### M6.1: Solution Scaffold

- [x] Create `services/WorkerService/WorkerService.sln`
- [x] Create `WorkerService.Api` project
- [x] Create `WorkerService.Application` project
- [x] Create `WorkerService.Domain` project
- [x] Create `WorkerService.Infrastructure` project
- [x] Add ProjectReferences to BuildingBlocks, Outbox, Contracts
- [x] Wire up DI composition
- [x] Add to root dev solution

### M6.2: Domain

- [-] Create `WorkerNode` entity (deferred — worker identity via environment vars)
- [x] Create `WorkerStatus` enum
- [x] Create `WorkerExecution` entity (local execution tracking)
- [x] Create `ExecutionStatus` enum (Running, Completed, Failed)
- [-] Create `ExecutionAttempt` value object (inlined into WorkerExecution)
- [x] Create `WorkerErrors` static error definitions
- [-] Domain rules: concurrency limit, graceful drain (deferred to M7 chaos)

### M6.3: Application

- [x] `HandleJobCreatedCommand` + handler (receive event → claim → execute)
- [x] `HandleJobRetryScheduledCommand` + handler
- [-] `ExecuteJobCommand` + handler (inlined into HandleJobCreated/RetryScheduled handlers)
- [-] `CompleteJobExecutionCommand` + handler (inlined — outcome written via outbox)
- [-] `FailJobExecutionCommand` + handler (inlined — outcome written via outbox)
- [-] `SendHeartbeatCommand` + handler (deferred to heartbeat hosted service)
- [-] `RecordExecutionOutcomeCommand` + handler (outcome via outbox interceptor)
- [x] `HandleJobCancelledCommand` + handler
- [x] Define `IJobHandler` interface
- [x] Define `JobExecutionContext` record
- [x] Define `IJobHandlerRegistry` interface
- [-] Create `JobExecutionCoordinator` (inlined into handlers)
- [x] Define `IJobServiceClient` (calls JobService internal APIs)
- [-] Create `IdempotencyService` (using IProcessedMessageRepository directly)
- [-] Create `ExecutionOutcomePublisher` (outbox interceptor handles publishing)
- [x] Define `IWorkerExecutionRepository`
- [x] Define `IProcessedMessageRepository`
- [x] Define `IUnitOfWork`

### M6.4: Job Handlers

- [x] `GenerateReportHandler` — creates artifact, idempotent
- [x] `DataExportHandler` — creates export artifact
- [x] `AlwaysFailHandler` — always throws, for permanent failure demo
- [x] `FailNTimesThenSucceedHandler` — configurable transient failure
- [x] `SlowJobHandler` — configurable delay, for lease/heartbeat demo

### M6.5: Infrastructure

- [x] Create `WorkerDbContext`
- [x] EF configuration for `WorkerExecution` (table: `worker_executions`)
- [x] EF configuration for `ProcessedMessage` (table: `processed_messages`)
- [x] Map outbox table via `modelBuilder.AddDaprOutbox()`
- [x] Create indexes from data model
- [x] Create initial migration (InitialCreate)
- [x] Implement `WorkerExecutionRepository`
- [x] Implement `ProcessedMessageRepository`
- [-] Implement `EfCoreOutboxEventWriter` (outbox handled by interceptor)
- [x] Register `AddDaprOutbox<WorkerDbContext>()`
- [x] Create `JobServiceClient` — DaprClient service invocation for claim/heartbeat
- [x] Create `WorkerHeartbeatHostedService` — heartbeats every 10s
- [x] Create `JobHandlerRegistry`
- [x] Create `DateTimeProvider`

### M6.6: Api — Endpoints

- [x] `POST /dapr/jobs/created` — Dapr subscription for JobCreated
- [x] `POST /dapr/jobs/retry-scheduled` — Dapr subscription for JobRetryScheduled
- [x] `POST /dapr/jobs/cancelled` — Dapr subscription for JobCancelled
- [x] `GET /health` — worker health
- [x] `GET /workers/status` — worker runtime status

### M6.7: Architecture Tests

- [x] Layer dependency tests
- [x] Domain must not duplicate Job state machine

---

## M7: Reliability + Chaos Middleware

### M7.1: Idempotency

- [x] `processed_messages` table in both services
- [x] Message deduplication by CloudEvent ID / outbox message ID
- [x] Job state machine rejects duplicate/invalid transitions
- [x] Optimistic concurrency on job claims (only one winner)
- [x] Deterministic idempotency key: `job:{jobId}:attempt:{attemptNumber}`
- [-] Idempotent artifact naming: `reports/{jobId}/attempt-{attemptNumber}.json` (deferred — no artifact storage yet)
- [x] **Chaos: duplicate event injection** toggle + test

### M7.2: Lease Recovery

- [x] `LeaseRecoveryBackgroundService` scans expired `LeaseUntil` every 5s
- [x] Expired Running → Retrying (if attempts remain) or Failed
- [x] Enqueue `JobRetryScheduled` through outbox on recovery
- [x] **Chaos: stop heartbeat** toggle + behavior (WorkerHeartbeatHostedService checks IChaosState)
- [x] **Chaos: crash after claim** toggle + behavior (ChaosCommandDecorator)

### M7.3: Retry with Backoff

- [x] Exponential backoff: 1s, 2s, 4s, 8s... capped at 60s
- [x] Jitter added to delay
- [x] `AttemptCount >= MaxAttempts` → terminal Failed
- [x] Manual retry: Failed → Retrying via `POST /jobs/{jobId}/retry`
- [x] **Chaos: fail next handler** toggle + behavior (ChaosCommandDecorator)
- [x] **Chaos: fail job type N times** toggle + behavior (ChaosCommandDecorator)

### M7.4: Chaos Middleware Framework

- [x] Define `IChaosState` interface (both services)
- [x] Implement `InMemoryChaosState` (thread-safe ConcurrentDictionary)
- [x] Define `ChaosPolicy` record (name, type, config)
- [-] `FailureInjectionMiddleware` — HTTP pipeline interception (not needed — using decorators)
- [x] `ChaosCommandDecorator<TCommand>` — command handler wrapping (WorkerService)
- [x] `ChaosEfInterceptor` — EF SaveChanges interception (crash after commit)
- [x] `ChaosOutboxDispatcherDecorator` — pause/resume/delay/fail outbox dispatcher
- [-] Guard: chaos only enabled in Development/Demo environment (deferred — always registered for demo)
- [x] Register all chaos components in InfrastructureModules

### M7.5: Chaos — JobService Toggles

- [x] `POST /demo/failures/crash-after-commit` — ChaosEfInterceptor throws after SaveChanges
- [x] `POST /demo/failures/pause-outbox` — ChaosOutboxDispatcherDecorator skips dispatch
- [x] `POST /demo/failures/resume-outbox` — deactivates pause-outbox policy
- [x] `POST /demo/failures/delay-outbox` — ChaosOutboxDispatcherDecorator adds delay
- [x] `POST /demo/failures/force-publish-failure` — ChaosOutboxDispatcherDecorator throws

### M7.6: Chaos — WorkerService Toggles

- [x] `POST /demo/failures/crash-after-claim` — ChaosCommandDecorator throws post-execution
- [x] `POST /demo/failures/crash-after-effect` — ChaosCommandDecorator throws post-execution
- [x] `POST /demo/failures/fail-next-job` — ChaosCommandDecorator throws (one-shot)
- [x] `POST /demo/failures/fail-job-type` — ChaosCommandDecorator throws N times for type
- [x] `POST /demo/failures/delay-execution` — ChaosCommandDecorator adds pre-execution delay
- [x] `POST /demo/failures/stop-heartbeat` — WorkerHeartbeatHostedService skips heartbeats
- [x] `POST /demo/failures/duplicate-event` — toggle exists (behavior at Dapr subscription level)
- [x] `GET /demo/failures` — list active chaos policies

---

## M8: Docker Compose Infrastructure

- [x] Create `deploy/compose/docker-compose.yml`
- [x] Postgres container with two databases: `jobservice_db`, `workerservice_db`
- [x] Redis container (Dapr pub/sub backend)
- [x] JobService container + Dapr sidecar
- [x] WorkerService container (3 replicas) + Dapr sidecars
- [x] Create `deploy/dapr/components/pubsub.yaml` (Redis)
- [-] Create `deploy/dapr/components/statestore.yaml` (deferred — chaos state is in-memory)
- [x] Create `deploy/dapr/components/resiliency.yaml`
- [x] Create Dockerfiles for JobService and WorkerService
- [x] Create EF migrations (InitialCreate for both services)
- [x] Auto-migrate on startup (MigrateAsync in Program.cs)
- [x] Create design-time DbContext factories for EF tooling
- [ ] Verify: `docker compose up` starts everything
- [ ] Verify: job creation → worker execution → completion flow works
- [ ] Verify: multiple worker replicas claim independently

---

## M9: Observability (Basic)

- [x] Add OpenTelemetry SDK to both services (1.18.0)
- [x] Configure `ActivitySource` and `Meter` per service (JobServiceDiagnostics, WorkerServiceDiagnostics)
- [-] Add span tags: `job_id`, `worker_id`, `job_type`, `attempt_number` (deferred — requires custom Activity spans in handlers)
- [x] Structured logging with Serilog (console + correlation_id middleware)
- [-] Add `correlation_id` propagation through outbox → Dapr → consumer (deferred — Dapr handles trace propagation)
- [x] Add OpenTelemetry Collector to Docker Compose (otel-collector-contrib:0.115.1)
- [x] Add Prometheus to Docker Compose (v3.1.0)
- [x] Add Tempo to Docker Compose (2.6.1)
- [x] Add Grafana to Docker Compose (11.4.0, auto-provisioned datasources + dashboards)
- [x] Job metrics: `jobs.created`, `jobs.completed`, `jobs.failed`, `jobs.cancelled`, `jobs.leases_recovered`, `jobs.claim_conflicts`
- [-] Outbox metrics: `outbox_pending_messages`, `outbox_published_total` (deferred — outbox package has DaprOutboxDiagnostics, needs wiring)
- [x] Worker metrics: `worker.jobs_claimed`, `worker.jobs_executed`, `worker.jobs_failed`, `worker.claim_conflicts`, `worker.heartbeats_sent`, `worker.duplicates_skipped`
- [x] Create basic Grafana dashboard (job throughput, worker activity, heartbeats, HTTP p99)

---

## M10: Demo Scenarios — Verification

Each scenario must be repeatable from a clean `docker compose up`:

- [ ] **Scenario 1: Happy Path** — submit 100 jobs, all complete, 0 lost
- [ ] **Scenario 2: Transactional Outbox** — crash after commit, restart, job completes
- [ ] **Scenario 3: Worker Crash** — kill replica, leases expire, jobs reclaimed and complete
- [ ] **Scenario 4: Duplicate Delivery** — inject duplicate, no duplicate effect
- [ ] **Scenario 5: Retry Then Succeed** — FailNTimes handler, shows backoff, completes
- [ ] **Scenario 6: Permanent Failure** — AlwaysFail handler, reaches max attempts, manual retry
- [ ] **Scenario 7: Pause Outbox** — pause, submit jobs, resume, all publish and complete

---

## M11: Demo Control Panel

- [x] Single-page HTML (minimal, no framework) — served from JobService wwwroot
- [x] Submit single job (type selector, payload, max attempts)
- [x] Submit bulk jobs (count, type)
- [x] Job summary counts (Pending, Running, Retrying, Completed, Failed, Cancelled)
- [x] Recent jobs table (last 20, with state badges, cancel/retry actions)
- [-] Worker status display (deferred — visible in Docker Compose logs)
- [-] Outbox pending count (deferred — visible in Grafana)
- [x] Failure injection buttons (all JobService + WorkerService toggles)
- [x] Active chaos policies display with deactivate buttons
- [x] Auto-refresh / polling (2s interval)
- [x] Event log panel with timestamped entries
- [x] JsonStringEnumConverter for proper enum serialization

---

## M12: Community Demo Polish

- [x] Write README.md with architecture overview and quick start
- [x] Create architecture diagram (Mermaid in README)
- [x] Write demo script (step-by-step narrative)
- [ ] Verify clean `docker compose up` → full demo flow
- [ ] Record short demo clip or screenshots
- [x] Add troubleshooting section to README
- [x] Add explanation connecting back to Dapr PR #1863

---

## Deferred (Post-Demo)

- [ ] MiniStack S3 artifact storage
- [ ] NuGet publishing for packages
- [ ] Kubernetes deployment
- [ ] Cloud deployment (AWS)
- [ ] Authentication and authorization
- [ ] Advanced dashboard
- [ ] Event versioning
- [ ] Benchmark suite (10,000 jobs)
- [ ] SqlServer claim strategy testing
