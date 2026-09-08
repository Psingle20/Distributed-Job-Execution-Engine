# Distributed Job Execution Engine Plan

## 1. Purpose

The goal is to build a production-style distributed job execution engine that demonstrates how Dapr building blocks, the Dapr .NET client transactional outbox APIs, and a standalone EF Core outbox package can be combined to solve real distributed-systems problems.

This project should be suitable for a Dapr community demo and also useful as a serious reference architecture for building reliable .NET microservices with Clean Architecture, DDD, CQRS, EF Core, Dapr, idempotency, leases, retries, failure recovery, and observability.

The project should not feel like a generic sample application. It should demonstrate one strong reliability story:

```text
Accept jobs reliably.
Persist job state and event intent atomically.
Publish events through Dapr.
Distribute work across horizontally scaled workers.
Recover from crashes, retries, duplicate deliveries, and expired leases.
Prove behavior with logs, traces, metrics, and failure injection.
```

## 2. Background

The Dapr .NET SDK PR added SDK-level transactional outbox helper APIs and metadata support. The EF Core based outbox implementation was identified during review as better suited for a standalone userland package instead of the core SDK.

Related contribution:

```text
Dapr .NET SDK PR #1863
https://github.com/dapr/dotnet-sdk/pull/1863
```

This showcase will use:

```text
Dapr .NET client contribution
  SDK-level outbox transaction and metadata APIs

Dapr.EntityFrameworkCore.Outbox
  Standalone EF Core outbox package extracted from local branch

CleanArchitecture.BuildingBlocks
  Reusable package based on the existing Clean CQRS template

Distributed Job Execution Engine
  Real end-to-end system proving the value of the above pieces
```

## 3. Design Principles

1. Keep every service independently understandable.
2. Use the Clean CQRS template per service, not one giant solution for everything.
3. Keep domain logic inside the service or bounded context that owns it.
4. Put only reusable, generic primitives in shared packages.
5. Keep integration contracts separate from domain objects.
6. Avoid unnecessary microservices.
7. Start with JobService and WorkerService only.
8. Treat Dapr as infrastructure, not as the domain model.
9. Do not assume exactly-once delivery.
10. Use idempotency to make at-least-once delivery safe.
11. Prefer optimistic concurrency over distributed locking.
12. Make failure behavior explicit and demonstrable.
13. Keep the first demo local and repeatable.
14. Use MiniStack and Docker Compose for local production-like infrastructure.
15. Add deployment options later, after the local architecture is solid.

## 4. Repository Layout

The showcase project should live in its own top-level folder:

```text
distributed-job-engine/
|
+-- packages/
|   |
|   +-- Dapr.EntityFrameworkCore.Outbox/
|   |
|   +-- CleanArchitecture.BuildingBlocks/
|   |
|   +-- JobEngine.Contracts/
|
+-- services/
|   |
|   +-- JobService/
|   |
|   +-- WorkerService/
|
+-- deploy/
|   |
|   +-- compose/
|   |
|   +-- dapr/
|   |   |
|   |   +-- components/
|   |
|   +-- ministack/
|   |
|   +-- observability/
|
+-- docs/
    |
    +-- architecture/
    |
    +-- adr/
```

This keeps the showcase isolated from the Dapr SDK repository structure while allowing local `ProjectReference` usage during development.

## 5. Solution Strategy

Avoid a single mega-solution that contains every service, every package, and every test project as one large application.

Each real service should follow the Clean CQRS template independently:

```text
ServiceName/
|
+-- ServiceName.sln
+-- src/
|   |
|   +-- ServiceName.Api
|   +-- ServiceName.Application
|   +-- ServiceName.Domain
|   +-- ServiceName.Infrastructure
|
+-- tests/
    |
    +-- ServiceName.Domain.Tests
    +-- ServiceName.Application.Tests
    +-- ServiceName.IntegrationTests
    +-- ServiceName.ArchitectureTests
```

Packages can have their own solutions or be included in a development solution later if convenient:

```text
packages/
|
+-- Dapr.EntityFrameworkCore.Outbox/
|   |
|   +-- Dapr.EntityFrameworkCore.Outbox.sln
|   +-- src/Dapr.EntityFrameworkCore.Outbox
|   +-- tests/Dapr.EntityFrameworkCore.Outbox.Tests
|
+-- CleanArchitecture.BuildingBlocks/
    |
    +-- CleanArchitecture.BuildingBlocks.sln
    +-- src/CleanArchitecture.BuildingBlocks
    +-- tests/CleanArchitecture.BuildingBlocks.Tests
```

For early development, a temporary root development solution may be useful:

```text
DistributedJobEngine.Dev.sln
```

But this should be treated as a convenience solution, not the architectural boundary.

## 6. Packages

### 6.1 Dapr.EntityFrameworkCore.Outbox

Purpose:

```text
Provide a reusable EF Core transactional outbox implementation for applications that publish integration events through Dapr pub/sub.
```

Package responsibilities:

```text
Persist outbox rows in the same EF Core transaction as business entities.
Support explicit outbox enqueueing.
Support domain-event based enqueueing through conventions.
Claim pending outbox messages safely.
Publish through Dapr pub/sub.
Retry failed publishes.
Dead-letter messages that exceed max attempts.
Expose health checks.
Support retention cleanup for processed messages.
Emit stable CloudEvent IDs for consumer-side deduplication.
```

Package non-responsibilities:

```text
No job engine concepts.
No worker concepts.
No clean architecture assumptions.
No service-specific state machine.
No business workflow rules.
```

Initial public surface expected from the local branch:

```text
OutboxMessage
DaprOutboxOptions
DaprOutboxSaveChangesInterceptor
PollingOutboxDispatcher<TDbContext>
IOutboxClaimStrategy
RelationalOutboxClaimStrategy
PostgreSqlOutboxClaimStrategy
SqlServerOutboxClaimStrategy
IOutboxMessageFactory
AttributeOutboxMessageFactory
DaprOutboxEventAttribute
DbContextExtensions.EnqueueOutbox(...)
ModelBuilderExtensions.AddDaprOutbox(...)
AddDaprOutbox<TDbContext>(...)
DaprOutboxHealthCheck
OutboxRetentionHostedService
```

### 6.2 CleanArchitecture.BuildingBlocks

Purpose:

```text
Provide generic primitives that make it easy to create services using the Clean CQRS template.
```

This package is the foundation for creating services consistently, but it should stay thin. It should not become a heavy framework that hides too much behavior.

Initial contents:

```text
Entity
AggregateRoot
IDomainEvent
IDomainEventHandler<T>
IHasDomainEvents
Result
Result<T>
Error
ErrorType
ValidationError
ICommand
ICommand<TResponse>
ICommandHandler<TCommand>
ICommandHandler<TCommand, TResponse>
IQuery<TResponse>
IQueryHandler<TQuery, TResponse>
ICommandDispatcher
IQueryDispatcher
ValidationDecorator
LoggingDecorator
IDependencyModule
IDateTimeProvider or TimeProvider adapter
Pagination primitives
Maybe architecture test helpers
```

Rules for this package:

```text
If every service would reasonably need it, it can go here.
If only the job engine needs it, keep it in the job engine service.
If it references EF Core, Dapr, Redis, HTTP, or Postgres, think carefully before adding it.
If it contains business language like Job, Worker, Lease, Report, or RetryAttempt, it does not belong here.
```

### 6.3 JobEngine.Contracts

Purpose:

```text
Hold integration event contracts shared across service boundaries.
```

This package is optional in the generic template, but useful for this showcase because JobService publishes messages consumed by WorkerService.

Contents:

```text
JobCreatedIntegrationEvent
JobRetryScheduledIntegrationEvent
JobCancelledIntegrationEvent
JobCompletedIntegrationEvent
JobFailedIntegrationEvent
```

Rules:

```text
Contracts are not domain objects.
Contracts should be stable and versionable.
Contracts should not contain behavior.
Contracts should not expose EF Core entities.
Contracts should not expose the Job aggregate.
```

Example:

```csharp
public sealed record JobCreatedIntegrationEvent(
    Guid JobId,
    string JobType,
    DateTimeOffset CreatedAt,
    int MaxAttempts);
```

## 7. Services

### 7.1 JobService

JobService owns the job lifecycle and job state.

Responsibilities:

```text
Create jobs.
Query jobs.
Cancel jobs.
Retry failed jobs.
Persist job state.
Persist job timeline.
Create integration events through the outbox.
Expose job health and status APIs.
Provide demo/failure toggles related to job creation and outbox behavior.
```

Layers:

```text
JobService.Api
JobService.Application
JobService.Domain
JobService.Infrastructure
```

JobService owns:

```text
Job aggregate
Job state machine
Retry policy
Lease rules from the job owner's perspective
Job repository abstraction
Job timeline
Outbox writes for job lifecycle events
```

JobService does not own:

```text
Actual job handler implementation
Worker process lifecycle
External side effects performed by workers
```

### 7.2 WorkerService

WorkerService consumes job events, claims jobs, executes registered handlers, and reports outcomes.

Responsibilities:

```text
Subscribe to Dapr pub/sub job events.
Deduplicate messages.
Claim jobs safely.
Run registered job handlers.
Heartbeat active leases.
Recover expired leases.
Apply retry decisions.
Report completion and failure.
Expose worker health.
Expose demo/failure toggles related to worker crashes and handler failures.
```

Layers:

```text
WorkerService.Api
WorkerService.Application
WorkerService.Domain
WorkerService.Infrastructure
```

WorkerService owns:

```text
Worker identity
Worker runtime status
Job handler registry
Execution coordination
Idempotency checks from the consumer perspective
Heartbeat background service
Worker execution attempt records
Worker outbox messages for execution outcomes
```

WorkerService does not own:

```text
The Job aggregate rules.
The canonical job state machine.
Canonical job lifecycle state.
Retry decisions for the Job aggregate.
The outbox package internals.
```

WorkerService reports execution signals back to JobService. JobService applies the canonical job state machine and persists final lifecycle transitions.

WorkerService should call JobService for lease-sensitive commands:

```text
POST /internal/jobs/{id}/claim
POST /internal/jobs/{id}/heartbeat
POST /internal/jobs/{id}/release
```

WorkerService should publish execution outcomes through its own outbox:

```text
WorkerService DB transaction
  Insert or update WorkerExecution
  Insert OutboxMessage(JobExecutionCompleted or JobExecutionFailed)

Dapr pub/sub
  WorkerService publishes execution outcome event

JobService
  Consumes execution outcome event
  Loads Job aggregate
  Applies Job state machine
  Persists Completed, Retrying, Failed, or Cancelled transition
```

### 7.3 DashboardService

DashboardService is optional for the first backend milestone.

Responsibilities:

```text
Show job counts.
Show worker status.
Show outbox pending count.
Show recent state transitions.
Trigger failure injection.
Submit bulk jobs.
Display live demo timeline.
```

The dashboard should not be built before the backend reliability story works. For the community demo, a minimal dashboard or Swagger plus Grafana may be enough.

## 8. High-Level Architecture

```text
                                  Client / Demo Dashboard
                                           |
                                           v
                                  +----------------+
                                  |   JobService   |
                                  | ASP.NET Core   |
                                  +--------+-------+
                                           |
                                           v
                                  PostgreSQL / RDS
                              +------------+-------------+
                              |                          |
                            Jobs                  OutboxMessages
                              |                          |
                              |                          v
                              |        Dapr.EntityFrameworkCore.Outbox
                              |                          |
                              |                          v
                              |                   Dapr Sidecar
                              |                          |
                              |                          v
                              |                 Pub/Sub Component
                              |                 Redis / MiniStack
                              |                          |
                  +-----------+------------+-------------+-----------+
                  |                        |                         |
                  v                        v                         v
          +---------------+        +---------------+         +---------------+
          | WorkerService |        | WorkerService |         | WorkerService |
          | replica 1     |        | replica 2     |         | replica N     |
          +-------+-------+        +-------+-------+         +-------+-------+
                  |                        |                         |
                  +-----------+------------+-------------+-----------+
                                           |
                                           v
                                WorkerService DB
                              +------------+-------------+
                              |                          |
                      WorkerExecutions           OutboxMessages
                              |                          |
                              |                          v
                              |        Dapr.EntityFrameworkCore.Outbox
                              |                          |
                              +------------+-------------+
                                           |
                                           v
                         Job execution result events through Dapr
                                           |
                                           v
                                  JobService subscriber
                                           |
                                           v
                              Job aggregate state machine
                                           |
                                           v
                                  JobService PostgreSQL / RDS
                                           |
                                           v
                                  MiniStack S3 artifacts
```

## 9. Local Infrastructure HLD

Local/demo infrastructure should be production-like but easy to run.

Use:

```text
Docker Compose
MiniStack
Dapr sidecars
PostgreSQL
Redis
OpenTelemetry Collector
Prometheus
Tempo
Grafana
```

MiniStack usage:

```text
RDS/Postgres
  Relational stores for JobService and WorkerService.
  JobService stores jobs, state transitions, processed messages, and job outbox rows.
  WorkerService stores worker executions, processed messages, and worker outbox rows.

ElastiCache/Redis
  Backend for Dapr pub/sub in the local demo.

S3
  Storage for generated job artifacts such as reports, exports, and demo output.
```

Dapr components:

```text
deploy/dapr/components/pubsub.yaml
  Pub/sub component backed by Redis.

deploy/dapr/components/statestore.yaml
  Optional state store for demo control flags.

deploy/dapr/components/resiliency.yaml
  Dapr resiliency policies for timeouts and retries.
```

Observability:

```text
OpenTelemetry Collector
  Receives traces and metrics from services.

Prometheus
  Scrapes metrics.

Tempo
  Stores traces.

Grafana
  Displays dashboards for jobs, workers, and outbox.
```

## 10. Overall Runtime Flow

### 10.1 Job Creation Flow

```text
1. Client sends POST /jobs.
2. JobService.Api receives request.
3. JobService.Application handles CreateJobCommand.
4. JobService.Domain creates Job aggregate in Pending state.
5. JobService.Infrastructure adds Job to EF Core DbContext.
6. Application enqueues JobCreatedIntegrationEvent through EF outbox.
7. EF Core SaveChanges commits Job and OutboxMessage atomically.
8. Outbox dispatcher claims pending outbox row.
9. Outbox dispatcher publishes JobCreated through Dapr pub/sub.
10. WorkerService receives JobCreated event.
11. WorkerService deduplicates by CloudEvent/outbox message ID.
12. WorkerService calls JobService internal claim endpoint.
13. JobService loads Job aggregate and applies the state machine.
14. JobService records the Running transition and returns execution lease details.
15. WorkerService records local WorkerExecution as Running.
16. WorkerService executes the registered job handler.
17. WorkerService records local WorkerExecution as Completed or Failed.
18. WorkerService enqueues JobExecutionCompleted or JobExecutionFailed through its own EF outbox.
19. WorkerService outbox publishes the execution outcome through Dapr pub/sub.
20. JobService receives the execution outcome event.
21. JobService deduplicates the message.
22. JobService loads Job aggregate.
23. JobService applies Complete, ScheduleRetry, or Fail through the state machine.
24. Metrics, traces, and job timeline are updated.
```

### 10.2 Execution Result Flow

```text
1. WorkerService executes a job handler.
2. WorkerService stores WorkerExecution result locally.
3. WorkerService enqueues execution outcome event through its local outbox.
4. WorkerService commits execution record and outbox message atomically.
5. WorkerService outbox dispatcher publishes through Dapr.
6. JobService receives JobExecutionCompleted or JobExecutionFailed.
7. JobService validates the message against the current Job state and lease owner.
8. JobService applies the canonical state transition.
9. JobService persists job state and job timeline.
10. JobService optionally publishes JobCompleted, JobFailed, or JobRetryScheduled through its own outbox.
```

Expected behavior:

```text
WorkerService can crash after recording the execution result without losing the completion/failure signal.
JobService remains the only owner of canonical job lifecycle state.
```

### 10.3 Worker Claim Flow

```text
1. WorkerService receives JobCreated or JobRetryScheduled.
2. WorkerService calls POST /internal/jobs/{jobId}/claim.
3. JobService checks message idempotency if needed.
4. JobService loads Job aggregate.
5. Job aggregate validates Pending or Retrying -> Running.
6. JobService assigns WorkerId and LeaseUntil.
7. JobService creates canonical JobExecution attempt record.
8. JobService persists Running transition.
9. WorkerService receives claim response.
10. WorkerService starts local execution.
```

If claim fails:

```text
Job is already claimed.
Job is completed.
Job is cancelled.
Job is failed.
Message is duplicate or stale.
```

WorkerService treats expected claim failures as safe no-ops.

### 10.4 Worker Heartbeat Flow

```text
1. WorkerService has active local execution.
2. WorkerService periodically calls POST /internal/jobs/{jobId}/heartbeat.
3. JobService loads Job aggregate.
4. Job aggregate validates the worker owns the current lease.
5. JobService extends LeaseUntil.
6. WorkerService continues execution.
```

If heartbeat fails:

```text
WorkerService marks local execution as uncertain.
WorkerService stops executing if the lease is known to be lost.
JobService remains canonical.
```

### 10.5 Lease Recovery Flow

```text
1. JobService background recovery scans Running jobs with expired LeaseUntil.
2. JobService loads each expired Job aggregate.
3. Job aggregate applies MarkLeaseExpired.
4. JobService transitions Running -> Retrying or Running -> Failed.
5. JobService records state transition.
6. JobService enqueues JobRetryScheduled through its own outbox when retry is allowed.
7. WorkerService receives retry event.
8. A worker claims and executes the job.
```

Expected demo result:

```text
Running jobs do not get stuck forever.
Recovery decisions are made by the service that owns the Job state machine.
```

### 10.6 Crash After Commit Flow

```text
1. Client submits job.
2. JobService commits Job and OutboxMessage.
3. Failure injection crashes JobService before publish.
4. Job remains Pending.
5. OutboxMessage remains unprocessed.
6. JobService restarts.
7. Outbox dispatcher finds pending message.
8. Message is published.
9. Worker executes job.
10. WorkerService records execution result and outbox message.
11. WorkerService publishes execution outcome after restart if needed.
12. JobService applies final job transition.
```

Expected demo result:

```text
No lost job creation event.
No lost execution result event.
```

### 10.7 Worker Crash Flow

```text
1. WorkerService replica 1 receives JobCreated.
2. WorkerService replica 1 calls JobService to claim the job.
3. JobService transitions job to Running with WorkerId and LeaseUntil.
4. WorkerService replica 1 records local WorkerExecution as Running.
5. WorkerService replica 1 crashes.
6. Heartbeat stops.
7. JobService lease recovery detects expired LeaseUntil.
8. JobService transitions Running -> Retrying.
9. JobService enqueues JobRetryScheduled through its own outbox.
10. Another worker receives retry event.
11. Another worker claims and executes the job.
12. WorkerService publishes execution outcome through its own outbox.
13. JobService consumes execution outcome and completes the job.
```

Expected demo result:

```text
Running jobs do not get stuck forever.
```

### 10.8 Duplicate Delivery Flow

```text
1. JobCreated message is delivered.
2. Worker starts processing.
3. Same message is delivered again.
4. WorkerService checks processed_messages and/or claim result.
5. JobService state machine allows only one claim.
6. Duplicate message is acknowledged safely.
```

Expected demo result:

```text
At-least-once delivery.
No duplicate side effect.
```

## 11. JobService LLD

### 11.1 Project Structure

```text
services/JobService/
|
+-- JobService.sln
+-- src/
|   |
|   +-- JobService.Api/
|   |
|   +-- JobService.Application/
|   |
|   +-- JobService.Domain/
|   |
|   +-- JobService.Infrastructure/
|
+-- tests/
    |
    +-- JobService.Domain.Tests/
    +-- JobService.Application.Tests/
    +-- JobService.IntegrationTests/
    +-- JobService.ArchitectureTests/
```

### 11.2 JobService.Domain

Domain folders:

```text
Jobs/
  Job.cs
  JobState.cs
  JobStateMachine.cs
  JobErrors.cs
  JobRetryPolicy.cs
  JobLease.cs
  JobStateTransition.cs
  JobType.cs
```

Core aggregate:

```csharp
public sealed class Job : AggregateRoot
{
    public Guid Id { get; private set; }
    public string Type { get; private set; }
    public string PayloadJson { get; private set; }
    public JobState State { get; private set; }
    public int AttemptCount { get; private set; }
    public int MaxAttempts { get; private set; }
    public string? WorkerId { get; private set; }
    public DateTimeOffset? LeaseUntil { get; private set; }
    public DateTimeOffset? LastHeartbeatAt { get; private set; }
    public DateTimeOffset? NextRunAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }
    public string? LastError { get; private set; }
    public uint Version { get; private set; }
}
```

States:

```text
Pending
Running
Retrying
Completed
Failed
Cancelled
```

Allowed transitions:

```text
Pending  -> Running
Pending  -> Cancelled

Running  -> Completed
Running  -> Retrying
Running  -> Failed
Running  -> Cancelled

Retrying -> Running
Retrying -> Failed
Retrying -> Cancelled

Failed   -> Retrying
Completed -> none
Cancelled -> none
```

Domain methods:

```text
Create(...)
Claim(workerId, now, leaseDuration)
Heartbeat(workerId, now, leaseDuration)
Complete(workerId, now)
Fail(workerId, now, error)
ScheduleRetry(workerId, now, retryAt, error)
Cancel(now)
MarkLeaseExpired(now, retryAt)
CanBeClaimed(now)
```

Domain rules:

```text
Only Pending or Retrying jobs can be claimed.
Only the current lease owner can heartbeat.
Only the current lease owner can complete or fail a Running job.
Completed jobs are terminal.
Cancelled jobs are terminal.
Failed jobs can move to Retrying only through an explicit retry use case.
AttemptCount cannot exceed MaxAttempts.
LeaseUntil must be in the future when a job is Running.
State cannot be assigned directly from application or infrastructure.
```

### 11.3 JobService.Application

Commands:

```text
CreateJobCommand
CancelJobCommand
RetryJobCommand
ClaimJobCommand
HeartbeatJobCommand
CompleteJobCommand
FailJobCommand
HandleJobExecutionCompletedCommand
HandleJobExecutionFailedCommand
RecoverExpiredLeasesCommand
```

Queries:

```text
GetJobByIdQuery
SearchJobsQuery
GetJobTimelineQuery
GetJobExecutionsQuery
GetJobSummaryQuery
```

Application abstractions:

```text
IJobRepository
IJobTimelineRepository
IProcessedMessageRepository
IUnitOfWork
IOutboxEventWriter
IJobArtifactStore
```

Application rules:

```text
Application orchestrates use cases.
Application does not bypass domain methods.
Application does not directly assign Job.State.
Application writes integration events through IOutboxEventWriter.
Application returns Result<T> for expected failures.
```

### 11.4 JobService.Infrastructure

Infrastructure folders:

```text
Database/
  JobDbContext.cs
  Configurations/
  Migrations/

Repositories/
  JobRepository.cs
  JobTimelineRepository.cs
  ProcessedMessageRepository.cs

Outbox/
  EfCoreOutboxEventWriter.cs

MiniStack/
  S3JobArtifactStore.cs

Dapr/
  DaprConfiguration.cs
```

DbContext responsibilities:

```text
Map Job aggregate.
Map JobExecution.
Map JobStateTransition.
Map ProcessedMessage.
Map Dapr outbox table.
Attach DaprOutboxSaveChangesInterceptor.
Configure optimistic concurrency token.
```

Outbox registration example:

```csharp
services.AddDaprOutbox<JobDbContext>(options =>
{
    options.TableName = "dapr_outbox_messages";
    options.PollInterval = TimeSpan.FromSeconds(1);
    options.BatchSize = 100;
    options.LockDuration = TimeSpan.FromSeconds(30);
    options.MaxAttempts = 10;
    options.HealthCheckThreshold = TimeSpan.FromMinutes(2);
});
```

Model configuration example:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobDbContext).Assembly);

    modelBuilder.AddDaprOutbox(options =>
    {
        options.TableName = "dapr_outbox_messages";
    });
}
```

### 11.5 JobService.Api

Public endpoints:

```text
POST   /jobs
GET    /jobs/{jobId}
GET    /jobs
POST   /jobs/{jobId}/cancel
POST   /jobs/{jobId}/retry
GET    /jobs/{jobId}/timeline
GET    /jobs/{jobId}/executions
GET    /jobs/summary
```

Internal/demo endpoints:

```text
POST   /demo/jobs/bulk
POST   /demo/failures/crash-after-commit
POST   /demo/failures/pause-outbox
POST   /demo/failures/resume-outbox
GET    /demo/outbox
```

Internal worker endpoints:

```text
POST   /internal/jobs/{jobId}/claim
POST   /internal/jobs/{jobId}/heartbeat
POST   /internal/jobs/{jobId}/release
```

Job completion and failure are not reported through direct internal HTTP endpoints in the preferred architecture. WorkerService records the execution outcome locally and publishes `JobExecutionCompletedIntegrationEvent` or `JobExecutionFailedIntegrationEvent` through its own outbox. JobService consumes those events and applies the state machine.

## 12. WorkerService LLD

### 12.1 Project Structure

```text
services/WorkerService/
|
+-- WorkerService.sln
+-- src/
|   |
|   +-- WorkerService.Api/
|   |
|   +-- WorkerService.Application/
|   |
|   +-- WorkerService.Domain/
|   |
|   +-- WorkerService.Infrastructure/
|
+-- tests/
    |
    +-- WorkerService.Domain.Tests/
    +-- WorkerService.Application.Tests/
    +-- WorkerService.IntegrationTests/
    +-- WorkerService.ArchitectureTests/
```

### 12.2 WorkerService.Domain

Domain folders:

```text
Workers/
  WorkerNode.cs
  WorkerStatus.cs

Executions/
  JobExecution.cs
  ExecutionStatus.cs
  ExecutionAttempt.cs
```

Worker domain rules:

```text
Worker identity is stable for the process lifetime.
Worker can run only up to configured concurrency.
Worker can gracefully drain active jobs during shutdown.
Worker can mark itself unhealthy if heartbeat extension repeatedly fails.
```

Important boundary:

```text
WorkerService.Domain should not duplicate the Job aggregate state machine.
The canonical Job state machine remains in JobService.Domain.
```

### 12.3 WorkerService.Application

Commands:

```text
HandleJobCreatedCommand
HandleJobRetryScheduledCommand
ExecuteJobCommand
CompleteJobExecutionCommand
FailJobExecutionCommand
SendHeartbeatCommand
RecordExecutionOutcomeCommand
```

Services:

```text
JobExecutionCoordinator
JobHandlerRegistry
WorkerLeaseService
IdempotencyService
WorkerRuntimeState
ExecutionOutcomePublisher
```

Job handler abstraction:

```csharp
public interface IJobHandler
{
    string JobType { get; }

    Task ExecuteAsync(
        JobExecutionContext context,
        CancellationToken cancellationToken);
}
```

Execution context:

```csharp
public sealed record JobExecutionContext(
    Guid JobId,
    string JobType,
    string PayloadJson,
    int AttemptNumber,
    string WorkerId,
    string IdempotencyKey);
```

Initial handlers:

```text
GenerateReportHandler
DataExportHandler
AlwaysFailHandler
FailNTimesThenSucceedHandler
SlowJobHandler
```

The demo handlers should create visible side effects, such as a report artifact in MiniStack S3, while remaining idempotent.

### 12.4 WorkerService.Infrastructure

Infrastructure folders:

```text
Dapr/
  JobEventSubscriber.cs
  DaprTopicConfiguration.cs

Persistence/
  WorkerDbContext.cs
  WorkerExecutionRepository.cs
  ProcessedMessageStore.cs

JobService/
  JobServiceClient.cs

Outbox/
  EfCoreOutboxEventWriter.cs

MiniStack/
  S3ReportArtifactWriter.cs

BackgroundServices/
  WorkerHeartbeatHostedService.cs

Chaos/
  FailureInjectionOptions.cs
  FailureInjectionState.cs
```

Subscription topics:

```text
jobs.created
jobs.retry-scheduled
jobs.cancelled
```

Dapr subscription endpoint:

```text
POST /dapr/jobs/created
POST /dapr/jobs/retry-scheduled
POST /dapr/jobs/cancelled
```

## 13. Data Model

JobService and WorkerService have separate persistence ownership even if they use the same local Postgres instance during development.

```text
JobService database/schema
  jobs
  job_executions
  processed_messages
  job_state_transitions
  dapr_outbox_messages

WorkerService database/schema
  worker_executions
  processed_messages
  dapr_outbox_messages
```

### 13.1 jobs

Owned by JobService.

```text
id uuid primary key
type varchar(128) not null
payload_json jsonb not null
state varchar(32) not null
attempt_count int not null
max_attempts int not null
worker_id varchar(128) null
lease_until timestamptz null
last_heartbeat_at timestamptz null
next_run_at timestamptz null
created_at timestamptz not null
started_at timestamptz null
completed_at timestamptz null
failed_at timestamptz null
last_error text null
version xmin / rowversion / concurrency token
```

Indexes:

```text
IX_jobs_state_next_run_at
IX_jobs_lease_until
IX_jobs_worker_id
IX_jobs_type_created_at
```

### 13.2 job_executions

Owned by JobService. This is the canonical execution-attempt history from the job lifecycle perspective.

```text
id uuid primary key
job_id uuid not null
worker_id varchar(128) not null
attempt_number int not null
status varchar(32) not null
started_at timestamptz not null
completed_at timestamptz null
error text null
```

Indexes:

```text
IX_job_executions_job_id
UX_job_executions_job_id_attempt_number
```

### 13.3 processed_messages

Each service owns its own `processed_messages` table. JobService uses it for execution outcome events. WorkerService uses it for job command events such as `JobCreated` and `JobRetryScheduled`.

```text
message_id uuid primary key
job_id uuid null
consumer varchar(128) not null
processed_at timestamptz not null
```

Indexes:

```text
IX_processed_messages_job_id
IX_processed_messages_processed_at
```

### 13.4 job_state_transitions

Owned by JobService.

```text
id uuid primary key
job_id uuid not null
from_state varchar(32) null
to_state varchar(32) not null
reason varchar(256) null
worker_id varchar(128) null
occurred_at timestamptz not null
```

Indexes:

```text
IX_job_state_transitions_job_id_occurred_at
```

### 13.5 worker_executions

Owned by WorkerService. This records what the worker process actually did locally before publishing an execution outcome event back to JobService.

```text
id uuid primary key
job_id uuid not null
job_execution_id uuid not null
worker_id varchar(128) not null
job_type varchar(128) not null
attempt_number int not null
status varchar(32) not null
idempotency_key varchar(256) not null
started_at timestamptz not null
completed_at timestamptz null
error text null
artifact_uri text null
outcome_published_at timestamptz null
```

Indexes:

```text
IX_worker_executions_job_id
IX_worker_executions_worker_id
UX_worker_executions_job_execution_id
UX_worker_executions_idempotency_key
```

### 13.6 dapr_outbox_messages

Owned by `Dapr.EntityFrameworkCore.Outbox`. Each service that uses the outbox owns its own outbox table in its own database/schema.

Expected columns:

```text
id uuid primary key
schema_version int
occurred_at timestamptz
pubsub_name varchar(256)
topic varchar(256)
content_type varchar(128)
payload bytea
metadata_json text
correlation_id varchar(128)
processed_at timestamptz null
attempt_count int
last_error text null
lock_owner varchar(128) null
locked_until timestamptz null
```

Indexes:

```text
IX_dapr_outbox_messages_unprocessed
```

## 14. Integration Events

Initial event contracts:

```text
JobCreatedIntegrationEvent
JobRetryScheduledIntegrationEvent
JobCancelledIntegrationEvent
JobExecutionCompletedIntegrationEvent
JobExecutionFailedIntegrationEvent
JobCompletedIntegrationEvent
JobFailedIntegrationEvent
```

Recommended topics:

```text
jobs.created
jobs.retry-scheduled
jobs.cancelled
job-executions.completed
job-executions.failed
jobs.completed
jobs.failed
```

Only publish events that have a consumer or demo value. Do not publish every internal state transition by default.

Event versioning strategy:

```text
Add Version property if needed.
Prefer additive changes.
Avoid renaming fields.
Avoid exposing domain internals.
Use stable topic names.
```

## 15. Idempotency Strategy

The system must assume at-least-once delivery.

Idempotency layers:

```text
Message idempotency
  Use CloudEvent ID or outbox message ID.
  Store processed message IDs in processed_messages.

State idempotency
  Job state machine rejects duplicate or invalid transitions.
  Optimistic concurrency ensures only one worker claim wins.

Side-effect idempotency
  Handlers receive deterministic idempotency key.
  External artifacts use deterministic names or unique constraints.
```

Example idempotency key:

```text
job:{jobId}:attempt:{attemptNumber}
```

For generated reports:

```text
s3://job-artifacts/reports/{jobId}/attempt-{attemptNumber}.json
```

For once-only effects:

```text
s3://job-artifacts/reports/{jobId}/final.json
```

## 16. Claiming and Concurrency

Worker claim should be atomic and owned by JobService. WorkerService requests a claim through an internal JobService endpoint; JobService loads the aggregate, validates the transition, and persists the claim.

Conceptual SQL:

```sql
UPDATE jobs
SET state = 'Running',
    worker_id = @workerId,
    lease_until = @leaseUntil,
    last_heartbeat_at = @now,
    started_at = COALESCE(started_at, @now),
    attempt_count = attempt_count + 1
WHERE id = @jobId
  AND state IN ('Pending', 'Retrying')
  AND attempt_count < max_attempts;
```

Only one worker should update the row.

If no row is updated:

```text
The job was already claimed.
The job was completed.
The job was cancelled.
The job exceeded max attempts.
The message was a duplicate.
```

JobService should return a clear claim result. WorkerService should treat expected claim failures as safe no-ops unless the response indicates an actual error.

## 17. Lease Strategy

Job lease fields:

```text
worker_id
lease_until
last_heartbeat_at
```

Lease settings:

```text
Lease duration: 30 seconds
Heartbeat interval: 10 seconds
Lease recovery interval: 5 seconds
```

Rules:

```text
A Running job must have WorkerId and LeaseUntil.
Only the owning worker can extend the lease.
If LeaseUntil is in the past, the job is reclaimable.
Recovered jobs should transition to Retrying unless max attempts are exhausted.
Lease recovery should enqueue JobRetryScheduled through the outbox.
```

## 18. Retry Strategy

Retry policy:

```text
Attempt 1 failure -> retry after 1 second
Attempt 2 failure -> retry after 2 seconds
Attempt 3 failure -> retry after 4 seconds
Attempt 4 failure -> retry after 8 seconds
Maximum delay cap -> 60 seconds
Add jitter -> yes
```

Retry terminal rule:

```text
If AttemptCount >= MaxAttempts, transition to Failed.
```

Manual retry:

```text
POST /jobs/{jobId}/retry
```

Manual retry should:

```text
Require job state Failed.
Reset or increment retry metadata according to explicit policy.
Transition Failed -> Retrying.
Enqueue JobRetryScheduled through outbox.
Record state transition.
```

## 19. Failure Injection

Failure injection should be available only in development/demo mode.

JobService failure toggles:

```text
Crash after database commit.
Pause outbox dispatcher.
Resume outbox dispatcher.
Delay outbox publishing.
Force outbox publish failure.
```

WorkerService failure toggles:

```text
Crash after receiving message.
Crash after claiming job.
Crash after external side effect.
Fail next job.
Fail job type N times before success.
Delay job execution.
Inject duplicate message handling path.
Stop heartbeat.
```

Demo endpoints:

```text
POST /demo/failures/crash-after-commit
POST /demo/failures/crash-after-claim
POST /demo/failures/fail-next-job
POST /demo/failures/fail-job-type
POST /demo/failures/stop-heartbeat
POST /demo/failures/duplicate-event
POST /demo/failures/pause-outbox
POST /demo/failures/resume-outbox
```

## 20. Observability

### 20.1 Tracing

Activity sources:

```text
JobService.Api
JobService.Application
JobService.Infrastructure
Dapr.EntityFrameworkCore.Outbox
WorkerService.Api
WorkerService.Application
WorkerService.Infrastructure
```

Expected trace:

```text
POST /jobs
  CreateJobCommand
    Job.Create
    EF transaction
      INSERT jobs
      INSERT dapr_outbox_messages
  Outbox dispatch
    Claim outbox batch
    Publish Dapr pub/sub event
  Worker receive event
    Deduplicate message
    Claim job
    Execute handler
    Complete job
```

Trace propagation:

```text
Use traceparent metadata where possible.
Preserve correlation ID in outbox row.
Include job_id, worker_id, job_type, attempt_number as span tags.
```

### 20.2 Metrics

Job metrics:

```text
jobs_created_total
jobs_completed_total
jobs_failed_total
jobs_cancelled_total
jobs_retried_total
jobs_recovered_total
job_end_to_end_latency_ms
job_queue_latency_ms
job_execution_latency_ms
```

Outbox metrics:

```text
outbox_pending_messages
outbox_published_total
outbox_failed_total
outbox_deadlettered_total
outbox_publish_latency_ms
outbox_oldest_pending_age_seconds
```

Worker metrics:

```text
worker_jobs_active
worker_jobs_completed_total
worker_jobs_failed_total
worker_claim_conflicts_total
worker_duplicate_messages_total
worker_lease_expired_total
worker_execution_latency_ms
```

### 20.3 Logging

Use structured logs with:

```text
job_id
job_type
worker_id
attempt_number
message_id
correlation_id
state_from
state_to
```

## 21. Demo Scenarios

### 21.1 Scenario 1: Transactional Outbox

Steps:

```text
1. Enable crash-after-commit.
2. Submit a job.
3. JobService commits Job and OutboxMessage.
4. JobService crashes before event publication.
5. Restart JobService.
6. Outbox dispatcher publishes pending message.
7. WorkerService receives event.
8. WorkerService claims the job through JobService.
9. WorkerService records execution result and publishes outcome through its own outbox.
10. JobService consumes the outcome and completes the job through the state machine.
```

Message:

```text
The event was not lost because the intent to publish was committed with the business state.
```

### 21.2 Scenario 2: Worker Crash Recovery

Steps:

```text
1. Start 3 WorkerService replicas.
2. Submit 100 or 1000 jobs.
3. Kill one worker after it claims jobs.
4. Wait for leases to expire.
5. Remaining workers recover abandoned jobs.
6. All jobs complete.
```

Message:

```text
Worker failures do not permanently strand Running jobs.
```

### 21.3 Scenario 3: Duplicate Delivery

Steps:

```text
1. Submit a job.
2. Inject duplicate JobCreated delivery.
3. Observe duplicate message count.
4. Observe only one successful job execution.
5. Observe no duplicate artifact.
```

Message:

```text
At-least-once delivery is safe because processing is idempotent.
```

### 21.4 Scenario 4: Retry Then Success

Steps:

```text
1. Submit a FailNTimesThenSucceed job.
2. Handler fails the first two attempts.
3. Job transitions through Retrying.
4. Third attempt completes.
5. Timeline shows attempts and backoff.
```

Message:

```text
Transient failures recover automatically.
```

### 21.5 Scenario 5: Permanent Failure and Manual Retry

Steps:

```text
1. Submit AlwaysFail job.
2. Handler fails until max attempts.
3. Job moves to Failed.
4. Operator triggers manual retry.
5. Job re-enters Retrying.
```

Message:

```text
Permanent failure is visible and operator-recoverable.
```

## 22. Development Milestones

### Milestone 1: Project Structure and Planning

Deliverables:

```text
Create distributed-job-engine folder.
Create packages, services, deploy, and docs folders.
Create architecture plan.
Create initial ADRs.
Decide exact service boundary approach for WorkerService and JobService state updates.
```

Acceptance criteria:

```text
Folder structure exists.
Architecture document exists.
Initial decisions are documented.
```

### Milestone 2: Extract Dapr.EntityFrameworkCore.Outbox

Deliverables:

```text
Create standalone package project.
Copy package code from local Dapr branch.
Copy tests from local Dapr branch.
Adjust namespaces and package metadata if needed.
Run unit tests.
Add README for package usage.
```

Acceptance criteria:

```text
Package builds.
Tests pass.
Package can be referenced locally.
Basic usage is documented.
```

### Milestone 3: Extract CleanArchitecture.BuildingBlocks

Deliverables:

```text
Create building blocks package.
Move generic clean architecture primitives.
Keep service-specific sample code out.
Add tests for Result, Error, dispatchers, and decorators.
Add README for service template usage.
```

Acceptance criteria:

```text
Package builds.
Tests pass.
Clean CQRS template can reference it.
No business-specific concepts leak into it.
```

### Milestone 4: Create JobService from Template

Deliverables:

```text
Create JobService solution.
Create Api, Application, Domain, Infrastructure projects.
Add package references.
Implement Job aggregate and state machine.
Implement create/get/cancel/retry use cases.
Add EF Core Postgres persistence.
Add migrations.
```

Acceptance criteria:

```text
POST /jobs creates Pending job.
GET /jobs/{id} returns job.
Domain tests validate state transitions.
Architecture tests validate layer dependencies.
```

### Milestone 5: Integrate Outbox into JobService

Deliverables:

```text
Reference Dapr.EntityFrameworkCore.Outbox.
Map outbox table.
Attach SaveChanges interceptor.
Enqueue JobCreatedIntegrationEvent on job creation.
Run outbox dispatcher.
Publish through Dapr pub/sub.
Add outbox health check.
```

Acceptance criteria:

```text
Job row and outbox row commit together.
Outbox dispatcher publishes JobCreated.
Crash-after-commit scenario works manually.
```

### Milestone 6: Create WorkerService from Template

Deliverables:

```text
Create WorkerService solution.
Create Api, Application, Domain, Infrastructure projects.
Add Dapr subscription endpoints.
Add job handler abstraction.
Add GenerateReportHandler.
Add worker identity.
Add basic job claim through JobService.
Add local WorkerExecution persistence.
Add WorkerService outbox for execution outcomes.
```

Acceptance criteria:

```text
Worker receives JobCreated.
Worker claims Pending job through JobService.
Worker executes handler.
Worker records local execution result.
Worker publishes execution outcome through its own outbox.
JobService consumes outcome and marks job Completed.
Multiple worker replicas can run.
```

### Milestone 7: Add Reliability Features

Deliverables:

```text
Processed message table.
Optimistic concurrency for job claims.
Job execution history.
Retry with exponential backoff.
Lease heartbeat.
Lease recovery.
Dead-letter/permanent failure behavior.
```

Acceptance criteria:

```text
Duplicate delivery does not duplicate effects.
Only one worker claims a job.
Worker crash results in lease recovery.
Transient failure retries and succeeds.
Permanent failure moves to Failed.
```

### Milestone 8: Add MiniStack Local Infrastructure

Deliverables:

```text
Docker Compose for services and Dapr sidecars.
MiniStack configuration.
Postgres/RDS setup.
Redis/ElastiCache setup.
S3 bucket setup.
Dapr component YAML files.
```

Acceptance criteria:

```text
One command starts local infrastructure.
JobService connects to Postgres.
Dapr pub/sub works.
Worker writes artifacts to S3-compatible local storage.
```

### Milestone 9: Add Observability

Deliverables:

```text
OpenTelemetry traces.
Metrics.
Structured logs.
Prometheus.
Tempo.
Grafana dashboards.
Outbox metrics.
Worker metrics.
Job lifecycle metrics.
```

Acceptance criteria:

```text
Trace shows POST /jobs through worker completion.
Dashboard shows job counts.
Dashboard shows outbox pending count.
Dashboard shows worker activity.
```

### Milestone 10: Add Failure Injection

Deliverables:

```text
Crash after commit.
Pause outbox.
Resume outbox.
Duplicate event.
Crash after claim.
Stop heartbeat.
Fail next job.
Delay job execution.
```

Acceptance criteria:

```text
Each demo scenario can be triggered deterministically.
System recovers according to expected behavior.
Failure injection is disabled outside development/demo mode.
```

### Milestone 11: Demo Dashboard

Deliverables:

```text
Submit jobs.
Submit bulk jobs.
View job status counts.
View recent job timeline.
View worker health.
View outbox pending count.
Trigger failure scenarios.
```

Acceptance criteria:

```text
Presenter can run the main demo without manually hitting every API.
Dashboard supports the community call narrative.
```

### Milestone 12: Community Demo Polish

Deliverables:

```text
README.
Architecture diagrams.
Demo script.
Troubleshooting guide.
Screenshots or short recorded clip.
Comparison with naive publish-after-save.
Explanation of Dapr PR connection.
```

Acceptance criteria:

```text
A new developer can run the demo.
The community call story is clear.
The project explains why the outbox package matters.
```

## 23. September 30 Demo Scope

Target a strong 50-70 percent version.

Must have:

```text
Dapr.EntityFrameworkCore.Outbox local package.
CleanArchitecture.BuildingBlocks local package.
JobService created from clean template.
WorkerService created from clean template.
Job creation API.
EF Core Postgres persistence.
Transactional outbox write.
Dapr pub/sub event publishing.
Worker subscription.
Worker claim through JobService.
WorkerService execution-result outbox.
JobService consumes execution outcome and completes job through state machine.
Optimistic concurrency.
Basic idempotency.
Basic retry.
Basic lease recovery.
Docker Compose or MiniStack local run.
README and demo script.
```

Nice to have:

```text
MiniStack S3 artifact output.
OpenTelemetry trace from job creation to completion.
Simple Grafana dashboard.
Simple demo dashboard.
Failure injection endpoints.
```

Defer if needed:

```text
NuGet publishing.
Kubernetes deployment.
Cloud deployment.
Authentication and authorization.
Advanced dashboard polish.
Multiple business services.
Advanced event versioning.
Full benchmark suite.
```

## 24. Suggested Demo Narrative

Opening:

```text
I recently contributed transactional outbox helper functionality to the Dapr .NET SDK.
During review, the EF Core implementation was identified as something that should live as a standalone userland package.
This demo shows how that package can be used in a real distributed job engine.
```

Problem:

```text
If a service saves business state and then publishes an event, a crash between those two steps can silently lose work.
```

Solution:

```text
Save the job and the outbox message in the same EF Core transaction.
Let a dispatcher publish through Dapr.
Use stable CloudEvent IDs and idempotency to handle duplicate delivery.
Use worker leases to recover from crashed workers.
```

Proof:

```text
Submit jobs.
Show outbox row.
Show event publish.
Show worker claim.
Show WorkerService execution-result outbox.
Show JobService consuming the outcome and completing the job.
Crash after commit.
Restart and show recovery.
Kill worker.
Show lease expiry and reclaim.
Inject duplicate.
Show duplicate ignored.
```

Final message:

```text
Dapr gives us useful distributed application building blocks.
The EF Core outbox package connects those building blocks to normal application persistence.
Together they make reliable distributed workflows easier to build and reason about.
```

## 25. Architecture Decision Records To Create

Initial ADRs:

```text
ADR-001-use-clean-architecture-per-service.md
ADR-002-keep-job-state-machine-inside-jobservice-domain.md
ADR-003-use-standalone-ef-core-outbox-package.md
ADR-004-use-ministack-for-local-demo-infrastructure.md
ADR-005-use-at-least-once-delivery-with-idempotent-consumers.md
ADR-006-use-worker-leases-instead-of-distributed-locks.md
ADR-007-make-contracts-package-optional-for-template.md
```

## 26. Open Decisions

1. Should JobService and WorkerService use separate local Postgres databases or separate schemas in the same local Postgres instance for the first demo?
2. Should `JobEngine.Contracts` be created immediately, or should integration events start in JobService.Application and be extracted once WorkerService consumes them?
3. Should the first local setup use plain Docker Compose Postgres/Redis first, then MiniStack, or MiniStack from the beginning?
4. Should the first demo use Swagger plus Grafana, or a custom dashboard?
5. Should the outbox package stay under `packages/` in this repo first, or be moved into its own repository before the community demo?

## 27. Recommended Decisions For First Implementation

Recommended for speed and clarity:

```text
Use JobService internal endpoints for claim and heartbeat.
Use WorkerService local persistence plus outbox for execution outcome events.
Keep JobService as the only owner of canonical job lifecycle state.
Create JobEngine.Contracts immediately because WorkerService consumes job events.
Use Docker Compose first, then wire MiniStack once the service flow works.
Use Swagger plus logs first, add Grafana next, custom dashboard last.
Keep packages local through ProjectReference until after the demo.
```

These decisions reduce schedule risk while preserving the production-grade architecture.

## 28. Definition Of Done For The First Community Demo

The first demo is ready when the following works repeatedly from a clean local run:

```text
1. Start infrastructure and services.
2. Submit 100 jobs.
3. Jobs are persisted.
4. Outbox messages are persisted.
5. Outbox dispatcher publishes events.
6. Multiple workers consume events.
7. Workers claim jobs without duplicate execution.
8. Jobs complete.
9. A forced worker crash results in lease recovery.
10. A duplicate message does not duplicate the job effect.
11. A forced transient failure retries and eventually succeeds.
12. Logs/traces/metrics show the story.
```

Target demo result:

```text
Submitted: 100
Completed: 100
Lost: 0
Duplicate effects: 0
Recovered after worker crash: yes
Recovered after outbox interruption: yes
```

Later benchmark target:

```text
Submitted: 10,000
Completed: 10,000
Lost: 0
Duplicate effects: 0
```
