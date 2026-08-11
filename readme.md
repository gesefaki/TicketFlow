# TicketFlow

TicketFlow is a modular ticket reservation system built with .NET.

The project explores how a ticketing platform can be structured as a modular
monolith with explicit business boundaries, a domain model, command-query
separation, and reliable asynchronous communication.

> [!IMPORTANT]
> TicketFlow is under active development.

## Architecture

TicketFlow is designed as a modular monolith.

Each business module owns:

- its domain model;
- application use cases;
- persistence;
- infrastructure;
- public integration contracts.

A module must not access another module's internal types, `DbContext`, or
database tables directly.

Shared technical foundations are placed in `BuildingBlocks`. They must remain
independent of business modules and must not contain module-specific rules.

```text
Business Modules
      │
      ├── use Domain building blocks
      ├── use CQS contracts
      └── communicate through explicit integration contracts

Building Blocks
      │
      └── never depend on business modules
```

## Repository Structure

```text
TicketFlow/
├── docs/
│   └── adr/                              # Architecture Decision Records
├── src/
│   ├── BuildingBlocks/
│   │   ├── CQS/
│   │   │   ├── Abstractions/             # CQS extension contracts
│   │   │   ├── Base/                     # MediatR pipeline behaviors
│   │   │   └── Primitives/               # Lightweight request contracts
│   │   └── Domain/                       # Shared domain primitives
│   └── Modules/
│       └── Reservations/
│           └── Domain/                   # Reservation aggregate and events
└── tests/
    ├── BuildingBlocks.CQS.UnitTests/
    └── Reservations.Domain.UnitTests/
```

## Technology

| Area                          | Technology                 |
| ----------------------------- | -------------------------- |
| Platform                      | .NET 10                    |
| Language                      | C#                         |
| Request pipeline              | MediatR                    |
| Distributed cache abstraction | `IDistributedCache`        |
| Testing                       | xUnit and FluentAssertions |
| Persistence                   | EF Core — planned          |
| Messaging                     | RabbitMQ — planned         |

## Documentation

| Document                                            | Purpose                                                  |
| --------------------------------------------------- | -------------------------------------------------------- |
| [Architecture Decision Records](docs/adr/README.md) | Important architectural decisions and their consequences |
| [Building Blocks](src/BuildingBlocks/README.md)     | Map and ownership rules for shared components            |
| [CQS](src/BuildingBlocks/CQS/README.md)             | Request pipeline contracts and behavior                  |
| [Domain](src/BuildingBlocks/Domain/README.md)       | Shared domain foundations                                |
| [Reservations](src/Modules/Reservations/README.md)  | Reservation lifecycle and business rules                 |
