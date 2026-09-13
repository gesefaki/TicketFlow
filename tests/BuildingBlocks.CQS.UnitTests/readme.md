# CQS Unit Tests

This project tests the CQS pipeline behaviors directly, without starting an
HTTP host, database, or distributed cache service.

## Structure

```text
BuildingBlocks.CQS.UnitTests/
├── Behaviors/
│   ├── CachingBehaviorTests.cs
│   ├── LoggingBehaviorTests.cs
│   └── ValidationBehaviorTests.cs
└── Helpers/
    └── TestLogger.cs
```

Tests use xUnit, FluentAssertions, and Moq. `TestLogger<T>` captures log levels,
formatted messages, and exception instances. Logging tests use
`DefaultHttpContext`; validation tests use mocked validators and a real
FluentValidation asynchronous rule. Handler spies record invocation count and
cancellation token forwarding.

## Coverage

### Caching

- A cache hit returns the stored response without invoking the handler.
- A cache miss invokes the handler and writes JSON with a one-minute absolute
  expiration.
- Cache read failures and invalid JSON fall back to handler execution.
- Cache write failures preserve the handler response.
- Cache read and write cancellation propagate the original exception.

### Validation

- No validators and successful validators allow handler execution and forward
  the cancellation token.
- Asynchronous validators are awaited sequentially before handler execution.
- Invalid results from multiple validators are collected by field without
  invoking the handler.
- Duplicate messages are removed within each field, and request-level errors
  preserve an empty field name.
- A real asynchronous validation rule is exercised for valid and invalid input.
- Validator exceptions and cancellation stop the pipeline and propagate unchanged.
- Handler exceptions and cancellation propagate with and without validators.
- Null handler responses are preserved, and errors do not leak between requests
  when the behavior is reused.

### Logging

- Successful execution invokes the handler once, preserves its response, and
  forwards the cancellation token.
- Start and completion entries use `Information` level, include HTTP request
  metadata, and contain no exception. Successful completion uses `completed`.
- Handler failures produce a start entry followed by an `Error` entry with
  `failed`, request metadata, and the original exception instance.
- The original exception is rethrown, and failed requests do not produce a
  successful completion entry.

## Coverage Limits

There are no dedicated tests for `PerformanceBehavior`, logging without an
HTTP context, or cancellation logging. These tests also do not verify behavior
registration/order through dependency injection or a real cache provider.

## Running Tests

Run commands from the repository root with the .NET 10 SDK installed:

```shell
dotnet test tests/BuildingBlocks.CQS.UnitTests/TicketFlow.BuildingBlocks.CQS.UnitTests.csproj
```

To run only validation cases:

```shell
dotnet test tests/BuildingBlocks.CQS.UnitTests/TicketFlow.BuildingBlocks.CQS.UnitTests.csproj --filter FullyQualifiedName~ValidationBehaviorTests
```

To run all solution tests:

```shell
dotnet test TicketFlow.sln
```

Add `--no-restore` when dependencies have already been restored.

## Related Documentation

- [CQS building block](../../src/BuildingBlocks/CQS/readme.md)
- [Project overview](../../readme.md)
