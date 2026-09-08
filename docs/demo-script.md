# Community Demo Script

**Duration**: ~20 minutes
**Prerequisites**: Docker Desktop running, ports 5000/5101-5103/3000 free

## Setup (2 min)

```bash
docker compose -f deploy/compose/docker-compose.yml up --build -d
```

Wait for all services to be healthy. Open two browser tabs:
- **Tab 1**: http://localhost:5000 (Demo Control Panel)
- **Tab 2**: http://localhost:3000 (Grafana)

## Part 1: Architecture Overview (3 min)

Walk through the architecture diagram in the README:
- **JobService** owns the job state machine and transactional outbox
- **WorkerService** (3 replicas) subscribes via Dapr pub/sub, claims jobs via `DaprClient.InvokeMethodAsync`
- **Dapr sidecars** handle pub/sub routing, service invocation, and mTLS
- **EF Core Outbox** ensures atomic commit + event publish (from Dapr SDK PR #1863)

Key points to emphasize:
- Clean Architecture — each service has independent Domain/Application/Infrastructure/Api layers
- The outbox is a general-purpose package extracted from the Dapr .NET SDK contribution
- `DaprClient` is used directly for service-to-service calls (not raw HTTP)

## Part 2: Happy Path (3 min)

1. In the Demo Panel, select job type **GenerateReport**
2. Click **Submit Bulk (50 jobs)**
3. Watch the summary counters: Pending -> Running -> Completed
4. Point out the recent jobs table showing which worker claimed each job
5. Switch to Grafana — show the throughput metrics and trace waterfall

**Talking point**: All 50 jobs complete with zero loss across 3 worker replicas. Optimistic concurrency ensures each job is claimed exactly once.

## Part 3: Transactional Outbox (4 min)

This is the core demo for PR #1863.

1. Click **Crash After Commit** in the JobService chaos panel
2. Submit a single job
3. Show the event log: "Job created" appears but the outbox never published — the process crashed after the DB commit
4. Click **Deactivate** to clear the chaos policy
5. Wait 2-3 seconds — the outbox dispatcher picks up the unpublished message
6. The job transitions to Running and then Completed

**Talking point**: Without the transactional outbox, this job would be "ghost created" — committed to the database but with no event ever published. The outbox pattern from PR #1863 guarantees the event is eventually published.

Now demonstrate the outbox pause/resume:

1. Click **Pause Outbox**
2. Submit 10 jobs — they all show as Pending (events are committed but not dispatched)
3. Click **Resume Outbox**
4. All 10 events publish in a burst and jobs complete

## Part 4: Worker Crash & Lease Recovery (4 min)

1. Click **Stop Heartbeat** in the WorkerService chaos panel
2. Submit a **SlowJob** (takes 15+ seconds)
3. A worker claims it and starts executing, but stops sending heartbeats
4. Point out the job stays in Running state
5. After 30 seconds, the lease expires — JobService's recovery scan detects it
6. The job transitions to Retrying (visible in the recent jobs table)
7. Click **Deactivate** to restore heartbeats
8. Another worker picks up the retried job and completes it

**Talking point**: Time-bounded leases (30s duration, 10s heartbeat, 5s recovery scan) provide automatic failure detection without distributed locks.

## Part 5: Idempotent Consumers (2 min)

1. Click **Duplicate Event** in the WorkerService chaos panel
2. Submit a job
3. Show the event log: the first delivery is processed, the duplicate is rejected
4. The job completes exactly once despite receiving the event twice

**Talking point**: Three layers of idempotency — CloudEvent ID dedup, state machine transition validation, and optimistic concurrency — ensure at-least-once delivery is safe.

## Part 6: Retry & Failure (2 min)

1. Submit a job with type **FailNTimesThenSucceed** (max attempts = 5)
2. Watch it cycle: Running -> Failed -> Retrying -> Running -> ... -> Completed
3. Point out the exponential backoff delays (1s, 2s, 4s)

Then:

1. Submit a job with type **AlwaysFail** (max attempts = 3)
2. Watch it exhaust all retries and reach terminal Failed state
3. Click **Retry** in the jobs table — it goes back to Retrying and eventually fails again

## Wrap-Up (2 min)

- Show the Grafana dashboard with all the metrics from the demo
- Recap: transactional outbox, worker leases, idempotent consumers, chaos injection
- Point to the ADRs in `docs/adr/` for architectural rationale
- Mention the project is built with Clean Architecture + CQRS (no MediatR — custom dispatchers with reflection caching)
- Connect back to Dapr SDK PR #1863 and the `Dapr.EntityFrameworkCore.Outbox` package

## Cleanup

```bash
docker compose -f deploy/compose/docker-compose.yml down -v
```
