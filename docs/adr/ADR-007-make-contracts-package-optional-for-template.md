# ADR-007: Make Contracts Package Optional for Template

## Status

Accepted

## Context

Services that communicate via events need shared integration event contracts. These could live inside one service's Application layer or in a shared contracts package.

## Decision

Create `JobEngine.Contracts` immediately as a shared package containing integration event records. This package is specific to the job engine showcase. In the generic Clean Architecture template, a contracts package is optional — services that don't share events don't need one.

## Consequences

- WorkerService can reference contracts without depending on JobService internals.
- Contracts are stable, versionable records with no behavior.
- Contracts do not expose domain aggregates or EF Core entities.
- Adding a new event type requires updating the contracts package.
- For the template: services without cross-service events skip this package entirely.
