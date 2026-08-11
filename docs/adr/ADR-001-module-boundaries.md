# ADR-001: Module Boundaries

- **Status:** Accepted
- **Implementation:** Partial
- **Date:** 2026-08-12

## Context

TicketFlow is designed by modular monolith. Each business module should own its domain model, application use cases, infrastructure, and data.

## Decision

Each business module is divided into the following layers:

- `Domain`
- `Application`
- `Infrastructure`
- `Contracts`, when public integration contracts are required

A module must not directly access another module's internal types, database tables, or `DbContext`.

Modules communicate through public contracts and integration events.

The `BuildingBlocks` projects contain reusable technical abstractions only. They must not contain business rules belonging to a specific module.

The Domain layer must not depend on Application, Infrastructure, EF Core, RabbitMQ, or other external technologies.

## Consequences

- Module boundaries are explicit and easier to maintain.
- Modules can evolve with less impact on each other.
- Cross-module workflows require explicit contracts.
- Some operation become eventually consistent.
