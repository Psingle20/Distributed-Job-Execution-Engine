# ADR-004: Use Docker Compose for Local Demo Infrastructure

## Status

Accepted

## Context

The demo requires PostgreSQL, Redis (Dapr pub/sub), Dapr sidecars, and observability tools. Options include plain Docker Compose, MiniStack (AWS-compatible local emulation), or a hybrid approach.

## Decision

Use plain Docker Compose first for all local infrastructure. MiniStack can be added later once the service flow is solid, primarily for S3-compatible artifact storage.

## Consequences

- Simpler initial setup with fewer moving parts.
- Faster iteration during development.
- One `docker compose up` starts everything.
- MiniStack S3 artifact storage is deferred to a later milestone.
- No AWS SDK dependencies needed initially.
