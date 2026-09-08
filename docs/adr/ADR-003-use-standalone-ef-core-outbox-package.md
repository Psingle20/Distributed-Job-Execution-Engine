# ADR-003: Use Standalone EF Core Outbox Package

## Status

Accepted

## Context

The Dapr .NET SDK PR #1863 contributed transactional outbox helper APIs. During review, the EF Core-based outbox implementation was identified as better suited for a standalone userland package rather than the core SDK.

## Decision

Use `Dapr.EntityFrameworkCore.Outbox` as a standalone package extracted from the local Dapr SDK branch (`backup/ef-core-outbox-full`). Both JobService and WorkerService use this package for their respective outbox tables.

## Consequences

- Atomic commit of business state and outbox message in a single EF Core transaction.
- No lost events on crash between commit and publish.
- Package remains generic — no job engine or business concepts.
- Each service owns its own outbox table in its own database.
- Consumer-side deduplication uses stable CloudEvent IDs generated from outbox message IDs.
- Package supports PostgreSQL-optimized claim strategy (`FOR UPDATE SKIP LOCKED`).
