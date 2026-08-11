# ADR-002: Domain Events and Integration Events

- **Status:** Proposed
- **Implementation:** Partial
- **Date:** 2026-08-12

## Context

Events used inside a module and messages exchanged between modules have different responsibilities and reliability requirements.

Publishing domain events directly to a message broker would couple the domain model to transport and external contracts.

## Decision

Domain events are internal to the module and are processed locally within the application process.

They represent changed in the domain model, such as:

- `ReservationCreatedDomainEvent`
- `ReservationConfirmedDomainEvent`
- `ReservationCancelledDomainEvent`
- `ReservationExpiredDomainEvent`

Domain events are not published directly to RabbitMQ.

When another module must be notified, a local domain event handler creates a separate integration event and stores it in the transactional outbox.

Only integration events are published to RabbitMQ.

Integration events are public contracts and must be versioned carefully.

## Consequences

- The domain model remains independent of RabbitMQ.
- Internal domain events can change without immediately breaking other modules.
- Mapping between domain events and integration events is required.
- Integration contracts must be maintained separately.
