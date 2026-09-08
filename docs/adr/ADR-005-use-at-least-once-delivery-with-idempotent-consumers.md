# ADR-005: Use At-Least-Once Delivery with Idempotent Consumers

## Status

Accepted

## Context

Distributed messaging systems cannot guarantee exactly-once delivery. We need a strategy that handles duplicate message delivery safely.

## Decision

Assume at-least-once delivery everywhere. Make all consumers idempotent through three layers:

1. **Message idempotency** — Store processed message IDs in `processed_messages` table, deduplicate by CloudEvent ID.
2. **State idempotency** — Job state machine rejects duplicate or invalid transitions; optimistic concurrency ensures only one claim wins.
3. **Side-effect idempotency** — Handlers receive deterministic idempotency keys; artifacts use deterministic names.

## Consequences

- Duplicate delivery does not produce duplicate side effects.
- No dependency on exactly-once messaging infrastructure.
- Small storage overhead for processed message tracking.
- Idempotency key format: `job:{jobId}:attempt:{attemptNumber}`.
- Requires discipline in handler implementation — all side effects must be idempotent.
