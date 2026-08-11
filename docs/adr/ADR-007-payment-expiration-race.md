# ADR-007: Race Condition Between Payment and Expiration

- **Status:** Proposed
- **Implementation:** Partial
- **Date:** 2026-08-12

## Context

Payment confirmation and reservation expiration may be processed at nearly the
same time.

Both operations attempt to change a reservation from `Pending` to a terminal
status. Without an explicit policy, the final result may be inconsistent.

## Decision

The system uses the **first successful database commit wins** rule.

A reservation can be confirmed only when:

```text
Status = Pending AND ConfirmedAt < ExpiresAt
```

A reservation can expire only when:

```text
Status = Pending AND ExpiredAt >= ExpiresAt
```

Confirmation at exactly `ExpiresAt` is not allowed. At that boundary,
expiration wins.

The following transitions are prohibited:

- `Confirmed` to `Expired`
- `Expired` to `Confirmed`
- `Cancelled` to `Confirmed`
- `Cancelled` to `Expired`

The Reservation record must use an optimistic concurrency token.

When concurrent operations attempt to update the same reservation, one
transaction succeeds. The losing operation reloads the current state and
either completes idempotently or is rejected according to the new status.

RabbitMQ message order does not determine the result.

## Consequences

- Only one terminal status can be committed.
- The result is decided atomically by the database.
- Already committed terminal states are not automatically reversed.
- Consumers must handle concurrency conflicts and rejected transitions.
- If payment must always win based on the provider's authorization timestamp,
  regardless of processing order, a separate reconciliation policy will be
  required.
