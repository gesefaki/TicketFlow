using TicketFlow.BuildingBlocks.Domain.Interfaces;
using TicketFlow.Reservations.Domain.Customers;
using TicketFlow.Reservations.Domain.Reservations;
using TicketFlow.Reservations.Domain.Seats;

namespace TicketFlow.Reservations.Domain.Events;

public sealed record ReservationCreatedDomainEvent(
    ReservationId ReservationId,
    CustomerId CustomerId,
    IReadOnlyCollection<SeatId> SeatIds,
    DateTimeOffset ReservedAt,
    DateTimeOffset ExpiresAt) 
    : IDomainEvent;