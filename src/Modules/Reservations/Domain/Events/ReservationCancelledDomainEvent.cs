using TicketFlow.BuildingBlocks.Domain.Interfaces;
using TicketFlow.Reservations.Domain.Reservations;

namespace TicketFlow.Reservations.Domain.Events;

/// <summary>
/// Indicates that a reservation has been cancelled.
/// </summary>
/// <param name="ReservationId">Identifier of the cancelled reservation.</param>
/// <param name="CancelledAt">Timestamp at which the reservation was cancelled.</param>
public sealed record ReservationCancelledDomainEvent(
    ReservationId ReservationId,
    DateTimeOffset CancelledAt)
    : IDomainEvent;
