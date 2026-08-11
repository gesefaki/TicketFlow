using TicketFlow.BuildingBlocks.Domain.Interfaces;
using TicketFlow.Reservations.Domain.Reservations;

namespace TicketFlow.Reservations.Domain.Events;

public sealed record ReservationConfirmedDomainEvent(
    ReservationId ReservationId,
    DateTimeOffset ConfirmedAt)
    : IDomainEvent;