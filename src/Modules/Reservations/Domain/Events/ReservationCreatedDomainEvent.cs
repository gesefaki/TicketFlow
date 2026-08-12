using TicketFlow.BuildingBlocks.Domain.Interfaces;
using System.Collections.Immutable;
using TicketFlow.Reservations.Domain.Customers;
using TicketFlow.Reservations.Domain.Reservations;
using TicketFlow.Reservations.Domain.Seats;

namespace TicketFlow.Reservations.Domain.Events;

/// <summary>
/// Indicates that a pending reservation has been created.
/// </summary>
/// <param name="ReservationId">Identifier of the created reservation.</param>
/// <param name="CustomerId">Identifier of the customer who owns the reservation.</param>
/// <param name="SeatIds">Immutable snapshot of the reserved seat identifiers.</param>
/// <param name="ReservedAt">Timestamp at which the reservation was created.</param>
/// <param name="ExpiresAt">Timestamp at which the reservation expires.</param>
public sealed record ReservationCreatedDomainEvent(
    ReservationId ReservationId,
    CustomerId CustomerId,
    ImmutableArray<SeatId> SeatIds,
    DateTimeOffset ReservedAt,
    DateTimeOffset ExpiresAt) 
    : IDomainEvent;
