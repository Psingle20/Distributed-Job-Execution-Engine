# ADR-008: Use Chaos Middleware for Failure Injection

## Status

Accepted

## Context

The demo's primary value is showing how the system recovers from failures. We need a way to inject failures deterministically during live demos without modifying business logic.

## Decision

Use a chaos middleware pattern with three components:

1. **`IChaosState`** — Thread-safe in-memory flag store holding active chaos policies, toggled via demo API endpoints.
2. **Interception points** — `FailureInjectionMiddleware` (HTTP pipeline), `FailureInjectionDecorator<TCommand>` (command handlers), `ChaosEfInterceptor` (EF SaveChanges).
3. **Named policies** — Each failure type is a named policy (`CrashAfterCommit`, `FailHandler`, `StopHeartbeat`, etc.) with its own interception point.

Chaos components are registered conditionally and only enabled in Development/Demo environments.

## Consequences

- Business logic never knows chaos exists — clean separation of concerns.
- Same architectural pattern used by production chaos engineering tools.
- Demo-friendly — toggle failures via HTTP endpoints during live presentation.
- Extensible — new failure types only need a new policy name and interception point.
- Safe — chaos is disabled by default and guarded by environment check.
- Future option: back `IChaosState` with Dapr state store for multi-replica chaos scenarios.
