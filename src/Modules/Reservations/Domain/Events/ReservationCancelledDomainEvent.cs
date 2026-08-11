using TicketFlow.BuildingBlocks.Domain.Interfaces;
using TicketFlow.Reservations.Domain.Reservations;

namespace TicketFlow.Reservations.Domain.Events;

public sealed record ReservationCancelledDomainEvent(
    ReservationId ReservationId,
    DateTimeOffset CancelledAt)
    : IDomainEvent;