# ADR-005: Database Isolation per Module

- **Status:** Proposed
- **Implementation:** Planned
- **Date:** 2026-08-12

## Context

A shared `DbContext` and shared database tables allow modules to bypass their
public contracts and access each other's data directly.

This creates hidden coupling and makes module boundaries difficult to enforce.

## Decision

Each module has:

- its own database schema;
- its own EF Core `DbContext`;
- its own migrations;
- its own outbox and inbox tables.

For example, the Reservations module uses the `reservations` schema and a
`ReservationsDbContext`.

A module must not query or update tables owned by another module.

Cross-module database foreign keys are not used. References to another module
are stored as identifiers only.

Data required from another module is received through public APIs, integration
events, or local read models.

## Consequences

- Data ownership is explicit.
- Module boundaries are enforced at the persistence level.
- Each module can evolve its schema independently.
- Cross-module queries require additional contracts or read models.
- The modules may still use the same physical database while remaining
  logically isolated.
