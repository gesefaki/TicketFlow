# ADR-006: Database-Backed Reservation Expiration

- **Status:** Proposed
- **Implementation:** Planned
- **Date:** 2026-08-12

## Context

A reservation is valid only until its `ExpiresAt` timestamp.

Using only RabbitMQ TTL or delayed messages is not reliable enough as the
source of truth. A message may be delayed, delivered more than once, or not
published before an application failure.

## Decision

The `ExpiresAt` value stored in the Reservations database is the source of
truth for expiration.

A background worker periodically selects reservations matching:

```text
Status = Pending AND ExpiresAt <= current time
```

The worker processes reservations in batches and calls the domain `Expire`
operation for each eligible reservation.

The status update and the corresponding integration event in the outbox are
stored in the same database transaction.

RabbitMQ TTL or delayed messages may be used as an optimization for faster
processing, but they must not be the only expiration mechanism.

The database scan remains the recovery mechanism after application or broker
failures.

## Consequences

- Expiration continues to work after application or RabbitMQ restarts.
- The database remains the source of truth.
- Expiration processing is idempotent.
- A small delay may exist between `ExpiresAt` and the persisted `Expired`
  status.
- The worker must use batching and concurrency control.
