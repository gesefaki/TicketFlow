using TicketFlow.BuildingBlocks.Domain.Interfaces;
using TicketFlow.Reservations.Domain.Reservations;

namespace TicketFlow.Reservations.Domain.Events;

/// <summary>
/// Indicates that a reservation has been confirmed.
/// </summary>
/// <param name="ReservationId">Identifier of the confirmed reservation.</param>
/// <param name="ConfirmedAt">Timestamp at which the reservation was confirmed.</param>
public sealed record ReservationConfirmedDomainEvent(
    ReservationId ReservationId,
    DateTimeOffset ConfirmedAt)
    : IDomainEvent;
