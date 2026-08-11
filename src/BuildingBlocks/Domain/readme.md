# Domain Building Block

The Domain building block contains the shared primitives used to construct
domain models across TicketFlow modules.

It provides common domain mechanics while keeping business rules inside their
owning modules.

The project has no dependency on persistence, messaging, HTTP, or request
processing infrastructure.

## Structure

```text
Domain/
├── Common/
│   └── DomainException.cs
├── Interfaces/
│   ├── IAuditable.cs
│   ├── IDomainEvent.cs
│   ├── IHasDomainEvents.cs
│   └── ISoftDeletable.cs
└── Models/
    ├── AggregateRoot.cs
    └── BaseEntity.cs
```

## Base Entity

`BaseEntity<TId>` provides common identity and audit properties:

- `Id`;
- `CreatedAt`;
- `UpdatedAt`.

The identifier type is generic, allowing modules to use strongly typed
identifiers instead of sharing primitive values such as `Guid` throughout the
domain model.

The base entity does not define persistence behavior. Mapping, value
generation, and audit timestamp updates belong to the owning module's
infrastructure.

## Aggregate Root

`AggregateRoot<TId>` extends `BaseEntity<TId>` and collects domain events raised
during business operations.

## Domain Events

`IDomainEvent` is a marker interface for events representing something that
already happened inside the domain model.

## Domain Exceptions

`DomainException` represents a violation of a business invariant or an invalid
state transition.

## Auditing

`IAuditable` defines common audit information for entities.

The Domain project defines the contract only. The infrastructure layer is
responsible for deciding how audit timestamps are populated and persisted.

Audit metadata must not be used as a replacement for explicit business
timestamps such as:

- `ConfirmedAt`;
- `CancelledAt`;
- `ExpiredAt`;
- `ExpiresAt`.

Business timestamps describe domain facts, while audit timestamps describe
persistence changes.

## Soft Deletion

`ISoftDeletable` provides a common contract for entities that may be logically
deleted without immediately removing their database record.

Implementing soft deletion is an explicit decision of the owning module. Not
every entity is required to support it.

## Dependency Rules

The Domain building block must not reference:

- business modules;
- Application layers;
- Infrastructure layers;
- ASP.NET Core;
- MediatR;
- EF Core;
- RabbitMQ;
- caching services.
- something else dependencies.

A business module's Domain project may reference this building block.

The reverse dependency is prohibited.

## What Belongs Here

Suitable components include:

- aggregate and entity mechanics;
- generic domain event contracts;
- common auditing contracts;
- general-purpose domain exceptions.

A component should be shared only after it has a stable, module-independent
meaning.

## Related Documentation

- [Building Blocks overview](../README.md)
- [Reservations module](../../Modules/Reservations/README.md)
- [ADR-001: Module Boundaries](../../../docs/adr/ADR-001-module-boundaries.md)
- [ADR-002: Domain and Integration Events](../../../docs/adr/ADR-002-domain-and-integration-events.md)
