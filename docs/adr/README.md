# Architecture Decision Records

This directory contains the Architecture Decision Records (ADRs) for
TicketFlow.

## Decision Index

| ADR                                                 | Decision                               | Status   | Implementation |
| --------------------------------------------------- | -------------------------------------- | -------- | -------------- |
| [ADR-001](ADR-001-module-boundaries.md)             | Module boundaries                      | Accepted | Partial        |
| [ADR-002](ADR-002-domain-and-integration-events.md) | Domain and integration events          | Proposed | Partial        |
| [ADR-003](ADR-003-at-least-once-delivery.md)        | At-least-once message delivery         | Proposed | Planned        |
| [ADR-004](ADR-004-transactional-outbox-inbox.md)    | Transactional outbox and inbox         | Proposed | Planned        |
| [ADR-005](ADR-005-database-isolation-per-module.md) | Database isolation per module          | Proposed | Planned        |
| [ADR-006](ADR-006-reservation-expiration.md)        | Database-backed reservation expiration | Proposed | Planned        |
| [ADR-007](ADR-007-payment-expiration-race.md)       | Payment and expiration race condition  | Proposed | Partial        |

## Decision Status

An ADR has one decision status.

| Status       | Meaning                                                         |
| ------------ | --------------------------------------------------------------- |
| `Proposed`   | The decision is being considered and may still change           |
| `Accepted`   | The decision has been approved as the project direction         |
| `Deprecated` | The decision is still recorded but should no longer be followed |
| `Superseded` | A newer ADR has replaced the decision                           |

An accepted ADR is not necessarily fully implemented. Decision status and
implementation progress are tracked separately.

## Implementation Status

| Status     | Meaning                                                 |
| ---------- | ------------------------------------------------------- |
| `Planned`  | Implementation has not started                          |
| `Partial`  | Some parts are present in the repository                |
| `Complete` | The decision is fully represented in the implementation |

Implementation status should describe the repository honestly. It must not be
changed to `Complete` until the relevant behavior is implemented and verified.

## ADR Structure

TicketFlow ADRs use the following structure:

```md
# ADR-XXX: Decision Title

- **Status:** Proposed
- **Implementation:** Planned
- **Date:** YYYY-MM-DD

## Context

What problem or architectural pressure requires a decision?

## Decision

What has been decided?

## Consequences

What benefits, limitations, and responsibilities follow from the decision?
```

ADRs should remain short. Detailed setup instructions and API documentation
belong in the relevant component README.

## Lifecycle

Once accepted, an ADR should not be silently rewritten when the architectural
direction changes.

Instead:

1. create a new ADR;
2. mark the previous ADR as `Superseded`;
3. add a link from the previous ADR to the new one;
4. explain why the decision changed.

Small corrections that do not change the decision may be applied directly.

## Related Documentation

- [Project overview](../../README.md)
- [Building Blocks](../../src/BuildingBlocks/README.md)
- [Reservations module](../../src/Modules/Reservations/README.md)
