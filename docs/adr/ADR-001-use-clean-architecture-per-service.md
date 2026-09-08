# ADR-001: Use Clean Architecture Per Service

## Status

Accepted

## Context

The distributed job engine consists of multiple services (JobService, WorkerService). We need to decide whether to use a single monolithic solution with shared layers or independent Clean Architecture per service.

## Decision

Each service follows the Clean Architecture / Clean CQRS template independently with its own solution and four-layer structure: Api, Application, Domain, Infrastructure.

## Consequences

- Each service is independently understandable, buildable, and deployable.
- Domain logic stays inside the bounded context that owns it.
- No accidental coupling between service internals.
- Some structural duplication across services (DI wiring, middleware), which is acceptable.
- Shared generic primitives live in `CleanArchitecture.BuildingBlocks` package, not in service code.
