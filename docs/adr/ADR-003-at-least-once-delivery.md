# ADR-003: At-Least-Once Message Delivery

- **Status:** Proposed
- **Implementation:** Planned
- **Date:** 2026-08-12

## Context

Updating a module's database and delivering a RabbitMQ message cannot be guaranteed as one distributed transaction.

## Decision

Integration events use **at-least-once delivery** semantics.

This means that a message not be silently lost, but iw may be delivered more than once.

Each integration event contains a globally unique `MessageId`.

Consumers must be idempotent and must not repeat a business operation when the same message is delivered again.

A consumer acknowledges a RabbitMQ message only after successful processing.

Failed messages are retried according to a retry policy. After the retry limit
is reached, the message is moved to a dead-letter queue.

Global message ordering is not guaranteed. When ordering matters, consumers
use the aggregate identifier and event version.

## Consequences

- Temporary failures do not cause message loss.
- Duplicate delivery is expected behavior.
- Every consumer must support idempotent processing.
- Failed messages require monitoring and operational handling.
