# CQS Building Block

The CQS building block provides reusable contracts and MediatR pipeline
behaviors for request processing.

CQS means **Command Query Separation**:

- commands express an intention to change system state;
- queries retrieve data without changing business state.

The current implementation focuses on cross-cutting request behavior rather
than defining a complete command and query model.

## Structure

```text
CQS/
├── Abstractions/
│   └── Cache/
│       └── ICacheKeyGenerator.cs
├── Base/
│   └── Behaviors/
│       ├── CachingBehavior.cs
│       └── LoggingBehavior.cs
└── Primitives/
    └── Interfaces/
        └── ICacheableQuery.cs
```

## Projects

### `TicketFlow.BuildingBlocks.CQS.Primitives`

Contains lightweight request contracts.

It currently defines:

```csharp
ICacheableQuery<TResponse>
```

A query implements this interface when its response may be stored in the
distributed cache.

The project references the MediatR contracts package but does not depend on
the pipeline implementation.

### `TicketFlow.BuildingBlocks.CQS.Abstractions`

Contains contracts required by CQS infrastructure.

It currently defines:

```csharp
ICacheKeyGenerator<TRequest>
```

Each cacheable request requires a corresponding implementation that creates a
stable and unique cache key.

### `TicketFlow.BuildingBlocks.CQS`

Contains the MediatR pipeline behavior implementations.

It references both `CQS.Primitives` and `CQS.Abstractions`.

## Request Pipeline

MediatR pipeline behaviors wrap a request handler and provide technical
cross-cutting concerns.

Conceptually, a request passes through the pipeline before reaching its
handler:

```text
Request
  │
  ├── Logging
  ├── Caching, for cacheable queries
  │
  ▼
Handler
  │
  ▼
Response
```

The exact order depends on dependency injection registration in the hosting
application.

## Dependency Rules

The CQS building block may depend on request-processing infrastructure.

## Testing

The CQS unit tests cover the behavior of the pipeline components.

Tests are located in:

```text
tests/BuildingBlocks.CQS.UnitTests/
```

## Related Documentation

- [Building Blocks overview](../README.md)
- [Architecture Decision Records](../../../docs/adr/README.md)
