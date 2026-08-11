using TicketFlow.BuildingBlocks.Domain.Interfaces;
using TicketFlow.Reservations.Domain.Reservations;

namespace TicketFlow.Reservations.Domain.Events;

public sealed record ReservationExpiredDomainEvent(
    ReservationId ReservationId,
    DateTimeOffset ExpiredAt)
    : IDomainEvent;