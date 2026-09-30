# Dapr Community Demo Runbook

**Target duration:** 18-20 minutes
**Primary audience:** Dapr community developers and maintainers
**Goal:** Show how Dapr building blocks and a standalone EF Core transactional outbox combine into a reliable distributed job execution engine.

## 1. Demo Story

This is not a tour of every endpoint or chaos switch. The live demo follows one job through the important distributed-system boundaries:

1. JobService creates the job and its integration event atomically.
2. The EF Core outbox publishes the event through Dapr pub/sub.
3. A worker uses Dapr service invocation to acquire an authoritative lease.
4. The job state machine records execution, retries, and terminal outcomes.
5. Recovery services handle delayed publication, transient failures, and silent workers.
6. Metrics, traces, logs, and database records make those guarantees visible.

### What the main demo proves

- Atomic persistence of business data and outgoing integration events
- Eventual publication after the outbox dispatcher is paused or unavailable
- Dapr pub/sub for asynchronous work distribution
- Dapr service invocation for synchronous claim and heartbeat operations
- Durable retries driven by the canonical job state machine
- Lease-based recovery when a worker stops making progress
- Execution fencing that rejects results from stale attempts
- Operational visibility through the control panel, PostgreSQL, and Grafana

### Claims to avoid

- Do not call the processing model exactly-once delivery. Dapr pub/sub is at-least-once; correctness comes from idempotency, state-machine guards, optimistic concurrency, and execution fencing.
- Do not say the standalone EF Core package was merged into the Dapr SDK. The original proposal was split during review.
- Do not say a chaos hook terminates an operating-system process unless the process is actually stopped.
- Do not describe cancellation as cooperative handler cancellation. The canonical state is protected, but an already-running handler is not interrupted.

## 2. Contribution Context

Open with this distinction because it connects the project to the Dapr contribution accurately:

- [Dapr .NET SDK PR #1863](https://github.com/dapr/dotnet-sdk/pull/1863) was merged with `DaprClient` helpers for Dapr's native state-store transactional outbox: metadata constants, transaction request extensions, and `OutboxTransactionBuilder`.
- The EF Core outbox from the original proposal was intentionally separated into the standalone `Dapr.EntityFrameworkCore.Outbox` package in this repository.
- This engine demonstrates the standalone package for services whose aggregates live in EF Core, while using `DaprClient` for pub/sub and service invocation.

Suggested wording:

> PR #1863 started as both DaprClient helpers and an EF Core outbox. Maintainer feedback clarified the ownership boundary: native state-store outbox helpers belong in the SDK, while an EF Core implementation should be a standalone community package. The merged PR contains the SDK helpers, and this project demonstrates the standalone EF Core package in a realistic distributed workflow.

## 3. Architecture Overview (2-3 minutes)

Show the high-level diagram from `README.md`, then the state machine from `docs/architecture-guide.md`. Keep the explanation to these boundaries.

### JobService

- Owns the canonical `Job` aggregate and lifecycle state machine.
- Writes the job and `jobs.created` outbox message in one PostgreSQL transaction.
- Grants time-bounded execution leases and issues a new `ExecutionId` for each attempt.
- Receives completion and failure events and validates them against the active execution.
- Runs background services for expired-lease recovery and durable retry dispatch.

### WorkerService

- Subscribes to `jobs.created`, `jobs.retry-scheduled`, and `jobs.cancelled` through Dapr pub/sub.
- Calls JobService through `DaprClient.InvokeMethodAsync` to claim work and renew leases.
- Stores a local execution record and its completion or failure event atomically through its own outbox.
- Publishes outcomes back to JobService asynchronously.

### Infrastructure

- PostgreSQL: one database per service, with durable outbox and processed-message records
- Redis Streams: local Dapr pub/sub component
- Dapr sidecars: pub/sub, service discovery, invocation, resiliency, and CloudEvents
- OpenTelemetry Collector, Prometheus, and Grafana: traces and metrics

### Key design sentence

> JobService owns truth, workers own execution, Dapr carries communication, and the outbox closes the database-to-message-broker failure gap.

## 4. Presenter Preparation

Complete this before the call, not while screen sharing.

### Required software and ports

- Docker Desktop
- The .NET SDK used by the solution
- Dapr CLI initialized with `dapr init --slim`
- Ports `5000`, `5101-5103`, `3000`, `9090`, `4317`, `5432`, and `6379` available

### Start infrastructure

```powershell
docker compose -f deploy/compose/docker-compose.yml up -d
```

Start JobService and the worker instances using the VS Code **Run All Services** task or the `dapr run` commands in `README.md`.

Open these tabs before presenting:

1. Demo control panel: `http://localhost:5000`
2. Grafana: `http://localhost:3000`
3. Optional PostgreSQL query window connected to `jobservice_db`
4. Optional JobService and WorkerService terminals, filtered to useful logs

### Preflight checklist

- `GET http://localhost:5000/health` returns `200`.
- Each active worker health endpoint returns `200`.
- The control panel shows no active chaos policies.
- Grafana is receiving current metrics.
- One `generate-report` job completes successfully.
- Databases are reset after rehearsal so counters and tables begin cleanly.
- A screenshot or short recording of lease recovery is ready as a fallback.

## 5. Main Live Flow

### Part 1: Happy Path - One Job Across Both Services (2 minutes)

Use a slightly slow job so the audience can see the transition instead of only the final state.

1. Select `slow-job`.
2. Set payload to `{"delaySeconds":5}`.
3. Set **Max Attempts** to `3`.
4. Click **Submit Single**.
5. Point to the counters as the job moves `Pending -> Running -> Completed`.
6. Show the WorkerService terminal receiving `jobs.created` and invoking JobService to claim it.

Explain the flow while it runs:

```text
POST /jobs
  -> Job + outbox row committed atomically
  -> outbox publishes jobs.created through Dapr
  -> worker receives the CloudEvent
  -> worker claims through Dapr service invocation
  -> handler executes
  -> worker outbox publishes job-executions.completed
  -> JobService transitions the job to Completed
```

**Say:**

> The asynchronous event tells workers that work is available, but it does not grant ownership. Ownership is established by the synchronous claim, where JobService creates the lease and ExecutionId.

Do not open every layer or class. The architecture slide has already established the Clean Architecture boundaries.

### Part 2: Transactional Outbox - Durable Publication (4 minutes)

This is the primary package demonstration.

1. Select `generate-report` and reset the payload to `{}`.
2. Click **Pause Outbox** in the JobService chaos panel.
3. Submit `10` jobs using **Submit Bulk**.
4. Show that all ten jobs exist and remain `Pending`.
5. If the database window is open, run the query below and show unprocessed outbox rows.
6. Click **Resume Outbox**.
7. Watch the jobs move through `Running` to `Completed` in a burst.
8. Show the Grafana throughput spike.

```sql
SELECT
    COUNT(*) FILTER (WHERE processed_at IS NULL) AS pending,
    COUNT(*) FILTER (WHERE processed_at IS NOT NULL) AS processed
FROM "DaprOutboxMessages";
```

**Say:**

> Pausing the dispatcher does not affect the transaction that creates each job. The jobs and their outgoing messages are already durable in PostgreSQL. Resuming dispatch drains the backlog through Dapr without recreating the jobs or relying on the original HTTP requests.

Then connect it to the real failure mode:

> The pause represents the same durability boundary as a temporary broker outage or dispatcher restart. The database transaction has completed, but publication can happen later without losing the event.

This is clearer and more deterministic than using **Crash After Commit** in the main live flow.

### Part 3: Durable Retry and State Machine (3-4 minutes)

1. Select `fail-n-times`.
2. Set payload to `{"failCount":2}`.
3. Set **Max Attempts** to `5`.
4. Click **Submit Single**.
5. Watch the attempt counter and states:

```text
Pending
  -> Running (attempt 1)
  -> Retrying
  -> Running (attempt 2)
  -> Retrying
  -> Running (attempt 3)
  -> Completed
```

6. Point out that the handler is stateless. It uses the authoritative attempt number returned by JobService, so the retry remains correct across worker replicas and process restarts.

Optional database query:

```sql
SELECT attempt_number, worker_id, status, error,
       started_at, completed_at
FROM job_executions
WHERE job_id = '<job-id>'
ORDER BY attempt_number;
```

**Say:**

> Retry timing is stored on the job as NextRunAt; it is not an in-memory delay owned by one worker. The retry survives restarts because JobService's background dispatcher finds due jobs and emits retry events through the outbox.

If time permits, submit `always-fail` with **Max Attempts** set to `3` and show retry exhaustion reaching terminal `Failed`. Omit this second job if the earlier sections ran long.

### Part 4: Silent Worker Failure and Lease Recovery (3-4 minutes)

Use this final reliability scenario only after rehearsing it with the exact worker topology used during the call.

#### Deterministic setup

Run only the worker exposed on `5101` for this scenario. Chaos state is process-local, so activating a policy on `5101` does not affect workers on `5102` or `5103`.

1. Click **Crash After Claim**.
2. Select `generate-report`, payload `{}`, and **Max Attempts** `3`.
3. Submit one job.
4. Show that it reaches `Running`: the claim succeeded and a lease was created.
5. Explain that the injected failure occurs before the job handler and no outcome event is sent.
6. Wait for the 30-second lease to expire and the 5-second recovery scan to run.
7. Show `Running -> Retrying` and the **Leases Recovered** Grafana counter increasing.
8. Deactivate `crash-after-claim`.
9. The durable retry dispatcher emits `jobs.retry-scheduled`; the next attempt claims a new `ExecutionId` and completes.

Use the wait to explain fencing:

> Every claim receives a new ExecutionId. Heartbeats and outcomes must carry the active value. If an old attempt later reports success after ownership has moved, JobService rejects it as stale instead of allowing it to overwrite the current execution.

**Important:** This policy simulates a silent failure after claim; it does not terminate the worker operating-system process. Phrase it as an injected silent worker failure, not a literal process crash.

#### Optional fencing proof

Only use this when it has been rehearsed and enough time remains:

1. Activate **Stop Heartbeat** on the single active worker.
2. Submit `slow-job` with `{"delaySeconds":45}`.
3. Allow the lease to expire and the job to enter `Retrying`.
4. Deactivate **Stop Heartbeat** so the retry can maintain its new lease.
5. When the original attempt reports with its old `ExecutionId`, show **Stale (Fencing)** increasing in Grafana.

The complete second attempt can continue in the background. The proof is rejection of the stale first attempt.

### Part 5: Observability and Wrap-Up (2 minutes)

Use Grafana to tie the scenarios together:

- **Created / Completed / Failed**: business outcomes
- **Job Throughput Over Time**: the outbox release burst
- **Worker Activity**: claims, successful executions, and failures
- **Leases Recovered**: automatic recovery from silent execution loss
- **Stale (Fencing)**: rejected outcomes from obsolete executions, if the optional proof ran
- **Heartbeats**: active lease renewal, if a slow job ran
- **HTTP Request Duration**: service-level behavior

Close with three points:

1. The merged SDK contribution makes Dapr's native state-store outbox safer and easier to compose through `DaprClient`.
2. The standalone EF Core package serves a different boundary: applications whose domain aggregates are persisted through EF Core.
3. This engine demonstrates that package inside a realistic system with Dapr pub/sub, service invocation, durable retries, leases, fencing, and observability.

Suggested closing sentence:

> The important result is not that failures disappear. It is that failures become durable, observable state transitions from which the system can recover without losing jobs or accepting stale results.

## 6. Scenarios Outside the Main Live Path

`docs/architecture-guide.md` remains the complete scenario catalogue. Keep these capabilities for questions, recorded material, or a longer workshop:

| Scenario | Why it is not in the main 20-minute flow |
|---|---|
| Bulk load across three workers | Useful scale-out evidence, but weaker than the reliability story unless per-worker history is visible on screen |
| Crash after commit | The hook throws after EF commit but does not terminate the process; pause/resume demonstrates the outbox boundary more clearly |
| Force publish failure | Similar lesson to pause/resume and therefore redundant in a short presentation |
| Crash after effect | Requires careful explanation of the side-effect boundary and handler idempotency |
| Duplicate CloudEvent | Do not demonstrate until there is a deterministic redelivery control and an end-to-end assertion that the same CloudEvent ID is reused |
| Cancellation | Canonical state is protected, but the current handler is not cooperatively interrupted |
| Targeted job-type chaos | Valuable for a workshop, not necessary for the core architecture story |
| Manual retry from terminal failure | Good backup interaction if the live demo finishes early |

## 7. Timing and Cut Order

| Section | Target | Cut if behind schedule? |
|---|---:|---|
| Introduction and architecture | 3 min | No |
| Happy path | 2 min | No |
| Transactional outbox | 4 min | No - this is the central demo |
| Durable retry | 3 min | No |
| Lease recovery | 4 min | Shorten the explanation, keep the result |
| Observability and wrap-up | 2 min | No |
| Questions / buffer | 2 min | Use as available |

First cut: `always-fail`.
Second cut: optional fencing proof.
Never cut: contribution context, outbox pause/resume, or the closing distinction between native Dapr outbox helpers and the standalone EF Core package.

## 8. Failure-Safe Presenter Notes

- If a job completes too quickly to narrate, use `slow-job` with a 5-second payload.
- If Grafana lags, continue with the control panel and database query; telemetry export is not part of job correctness.
- If a chaos policy behaves unexpectedly, clear all policies and continue to the retry scenario.
- If lease recovery is slow, explain the 30-second lease plus 5-second scan interval while showing `lease_until`.
- If a Dapr sidecar fails, use the architecture diagram and prepared recording instead of debugging live.
- Keep terminal text large and pre-position database queries before screen sharing.

## 9. Cleanup

Stop the `dapr run` terminals, then remove local infrastructure and demo data:

```powershell
docker compose -f deploy/compose/docker-compose.yml down -v
```
