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
│   ├── Cache/
│   │   └── ICacheKeyGenerator.cs
│   └── Exceptions/
│       └── Validation/
│           └── RequestValidationException.cs
├── Base/
│   └── Behaviors/
│       ├── CachingBehavior.cs
│       ├── LoggingBehavior.cs
│       ├── PerformanceBehavior.cs
│       └── ValidationBehavior.cs
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

`RequestValidationException` represents request validation failures. Its
`Errors` property exposes an `IReadOnlyDictionary<string, string[]>` containing
messages grouped by field name. An empty field name represents a request-level
error. The exception itself does not define HTTP response handling.

### `TicketFlow.BuildingBlocks.CQS`

Contains the MediatR pipeline behavior implementations.

It references both `CQS.Primitives` and `CQS.Abstractions`.

### Behaviors

| Behavior | Current responsibility |
| -------- | ---------------------- |
| `LoggingBehavior` | Logs request start, successful completion, and handler exceptions |
| `ValidationBehavior` | Runs FluentValidation validators before invoking the next delegate |
| `PerformanceBehavior` | Warns when successful execution of the next delegate takes more than 700 ms |
| `CachingBehavior` | Reads and stores responses for cacheable queries using `IDistributedCache` |

#### Logging

Start and successful completion are logged at `Information` level, including
the request type, HTTP method, and path. When there is no HTTP context, the
method and path fall back to `Undefined`.

Exceptions from the next delegate are logged at `Error` level and rethrown
without replacement. This currently includes cancellation exceptions. A failed
request does not produce a successful completion entry.

#### Validation

Validators run sequentially using `ValidateAsync`, with the request and
cancellation token passed to each validator. An invalid validation result does
not stop the remaining validators from running.

Errors are collected by field name, and duplicate messages are removed within
each field. If any errors exist, the behavior throws
`RequestValidationException` and does not invoke the next delegate. It does
not write a validation log itself; details are available through `Errors`.

When there are no validators, or all validators succeed, the next delegate is
invoked with the same cancellation token. Exceptions and cancellation from a
validator propagate unchanged and stop further processing. Exceptions from the
next delegate also propagate unchanged.

#### Performance

The behavior measures the execution of the next delegate with `Stopwatch`.
After successful completion, elapsed time greater than 700 ms produces a
`Warning` entry containing the request type and duration. The threshold is
currently fixed in code.

If the next delegate throws or is canceled, the exception propagates and no
duration warning is written. Measured work depends on the behavior's position
in the pipeline and may include inner behaviors.

#### Caching

Only requests implementing `ICacheableQuery<TResponse>` use this behavior.
Their `ICacheKeyGenerator<TRequest>` supplies the cache key.

A valid, non-null JSON response from the cache is returned without invoking
the next delegate. On a cache miss, invalid JSON, or a deserialized null value,
the next delegate runs and its response is serialized using `System.Text.Json`.
Cache entries currently have a fixed absolute expiration of one minute.

Cache read/write and serialization failures are logged and normally allow
request processing to continue. `OperationCanceledException` from cache reads
or writes propagates. Exceptions from the next delegate also propagate, and
its failed execution is not cached. Cache invalidation is not implemented here.

## Request Pipeline

MediatR pipeline behaviors wrap a request handler and provide technical
cross-cutting concerns.

One possible registration order is shown below; it is illustrative, not an
order enforced by this library:

```text
Request
  │
  ├── Logging
  ├── Validation
  ├── Performance
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

Placing validation outside caching ensures validation also runs on cache hits.
Placing logging outside validation allows validation exceptions to be logged.
The host must register validators, behavior dependencies, and a key generator
for each cacheable request it uses.

## Dependency Rules

The CQS building block may depend on request-processing infrastructure.

## Testing

The CQS unit tests directly exercise caching, logging, and validation behaviors.
Dedicated performance and dependency injection pipeline tests are not currently
present.

Tests are located in:

```text
tests/BuildingBlocks.CQS.UnitTests/
```

See the [CQS unit test README](../../../tests/BuildingBlocks.CQS.UnitTests/readme.md)
for coverage, execution commands, and coverage limits.

## Related Documentation

- [Building Blocks overview](../readme.md)
- [Architecture Decision Records](../../../docs/adr/README.md)
