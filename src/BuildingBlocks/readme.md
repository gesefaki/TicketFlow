# Building Blocks

`BuildingBlocks` contains reusable, module-independent foundations shared
across TicketFlow.

Its purpose is to prevent repeated implementation of the same technical
mechanics while preserving the autonomy of business modules.

This document is a map of the available building blocks and the rules that
protect their boundaries.

## Scope

A component belongs in `BuildingBlocks` only when it is:

- independent of a specific business module;
- reusable by more than one part of the system;
- focused on a clear technical responsibility;
- safe to expose as a shared contract;
- testable without starting the complete application.

Reusable code is not automatically shared code.

If an abstraction originates from a single module and has no second consumer,
it should usually remain inside that module until a real common requirement
appears.

## Structure

```text
src/BuildingBlocks/
├── CQS/
│   ├── Abstractions/
│   ├── Base/
│   └── Primitives/
└── Domain/
```

## Available Building Blocks

### CQS

CQS provides contracts and MediatR pipeline behaviors for processing requests.

CQS is separated into smaller projects so consumers can depend only on the
contracts they require.

Detailed documentation: [CQS](CQS/readme.md).

### Domain

Domain provides the common mechanics required by module-owned domain models.

Its current responsibilities include:

- base entity identity and audit timestamps;
- aggregate root behavior;
- collection of domain events;
- shared domain interfaces;
- representation of domain rule violations.

It does not define reservation, payment, customer, or ticketing rules.

Detailed documentation: [Domain](Domain/readme.md).

## Dependency Rules

The fundamental dependency rule is:

```text
Business modules ───────> Building Blocks
Building Blocks ──X─────> Business modules
```

A Building Block must not reference:

- a business module;
- a module-specific entity or event;
- a module-specific database context;
- a module-specific application use case.

Additional rules apply to individual building blocks.

For example, the Domain building block must not depend on ASP.NET Core,
MediatR, EF Core, RabbitMQ, or other infrastructure technologies.

## Related Documentation

- [Project overview](../../readme.md)
- [CQS](CQS/readme.md)
- [Domain](Domain/readme.md)
- [Architecture Decision Records](../../docs/adr/README.md)
