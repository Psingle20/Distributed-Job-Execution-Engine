# ADR-002: Keep Job State Machine Inside JobService.Domain

## Status

Accepted

## Context

Both JobService and WorkerService interact with job lifecycle state. WorkerService claims jobs, executes them, and reports outcomes. We need to decide where the canonical job state machine lives.

## Decision

The canonical job state machine lives exclusively in `JobService.Domain`. WorkerService does not duplicate the state machine. WorkerService reports execution outcomes through its own outbox; JobService consumes those events and applies the state machine transitions.

## Consequences

- Single source of truth for job lifecycle rules.
- WorkerService cannot accidentally bypass state machine guards.
- WorkerService calls JobService internal HTTP endpoints for claim/heartbeat/release (synchronous, needs immediate answer).
- WorkerService publishes execution outcomes via its own outbox (asynchronous, durable).
- If state machine rules change, only JobService.Domain needs updating.
