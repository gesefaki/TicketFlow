# ADR-004: Transactional Outbox and Inbox

- **Status:** Proposed
- **Implementation:** Planned
- **Date:** 2026-08-12

## Context

Saving a business change to the database and publishing an integration event
to RabbitMQ are two separate operations.

If the application fails between them, the database may be updated without the
event being published. Publishing first creates the opposite risk.

## Decision

Each module uses the transactional outbox pattern.

A business change and its integration event are stored in the same database
transaction. The event is stored in an `OutboxMessages` table.

A background publisher reads pending outbox messages, publishes them to
RabbitMQ, and marks them as published.

Consumers use the transactional inbox pattern.

Before applying a received message, the consumer checks its `MessageId` in the
`InboxMessages` table. The processed message identifier and related business
changes are stored in the same transaction.

If the `MessageId` already exists, the duplicate message is acknowledged
without repeating the business operation.

Old outbox and inbox records are removed according to a retention policy.

## Consequences

- Database changes and outgoing events cannot become inconsistent.
- Consumers can safely handle duplicate messages.
- Message delivery remains at-least-once rather than exactly-once.
- Additional tables, background processing, and cleanup are required.
