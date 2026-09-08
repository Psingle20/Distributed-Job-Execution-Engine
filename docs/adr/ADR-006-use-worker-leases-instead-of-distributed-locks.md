# ADR-006: Use Worker Leases Instead of Distributed Locks

## Status

Accepted

## Context

When multiple worker replicas compete for jobs, we need a mechanism to ensure only one worker executes a given job at a time, and that crashed workers don't permanently strand jobs.

## Decision

Use time-bounded worker leases instead of distributed locks. A Running job has a `WorkerId` and `LeaseUntil` timestamp. Workers must heartbeat to extend their lease. Expired leases are reclaimed by JobService's recovery process.

Lease settings: 30s duration, 10s heartbeat interval, 5s recovery scan interval.

## Consequences

- No external distributed lock infrastructure (Redis, ZooKeeper) required.
- Crashed workers are detected by lease expiry, not by connection loss.
- JobService remains the canonical owner of lease state.
- Workers must heartbeat reliably — network partitions can cause lease loss.
- Optimistic concurrency prevents race conditions during claims.
- Recovery is automatic — no manual intervention needed for crashed workers.
