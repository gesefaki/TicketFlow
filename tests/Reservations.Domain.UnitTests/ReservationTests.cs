using FluentAssertions;
using TicketFlow.BuildingBlocks.Domain.Common;
using TicketFlow.Reservations.Domain.Customers;
using TicketFlow.Reservations.Domain.Reservations;
using TicketFlow.Reservations.Domain.Seats;

namespace TicketFlow.Reservations.Domain.UnitTests;

public class ReservationTests
{
    private readonly SeatId _seatId = SeatId.New();
    private readonly CustomerId _customerId = CustomerId.New();
    private readonly DateTimeOffset _reservedAt = DateTimeOffset.UtcNow;
    private readonly TimeSpan _reservationDuration = TimeSpan.FromMinutes(15);

    private readonly SeatId _emptySeatId = SeatId.Empty;
    private readonly CustomerId _emptyCustomerId = CustomerId.Empty;
    private readonly TimeSpan _zeroReservationDuration = TimeSpan.Zero;
    
    [Fact]
    public void Reserve_WithValidValues_ReturnCorrectReservation()
    {
        // Act
        var reservation =
            Reservation.Reserve([_seatId], _customerId, _reservedAt, _reservationDuration);

        // Assert
        reservation.Id.Should().NotBe(ReservationId.Empty);

        reservation.SeatIds.Should().ContainSingle()
            .Which.Should().Be(_seatId);

        reservation.CustomerId.Should().NotBe(CustomerId.Empty);
        Assert.Equal(reservation.CustomerId, _customerId);

        Assert.Equal(reservation.CreatedAt, _reservedAt);
        Assert.Equal(reservation.ExpiresAt, _reservedAt + _reservationDuration);

        reservation.Status.Should().Be(ReservationStatus.Pending);
    }

    [Fact]
    public void Reserve_WithMultipleSeatIds_ReserveEverySeat()
    {
        // Arrange
        var secondSeatId = SeatId.New();
        SeatId[] seatIds = [_seatId, secondSeatId];

        // Act
        var reservation =
            Reservation.Reserve(seatIds, _customerId, _reservedAt, _reservationDuration);

        // Assert
        reservation.SeatIds.Should().BeEquivalentTo(seatIds);
    }

    [Fact]
    public void Reserve_WhenSourceCollectionChanges_PreserveReservedSeatIds()
    {
        // Arrange
        var seatIds = new List<SeatId> { _seatId };
        var reservation =
            Reservation.Reserve(seatIds, _customerId, _reservedAt, _reservationDuration);

        // Act
        seatIds.Add(SeatId.New());

        // Assert
        reservation.SeatIds.Should().ContainSingle()
            .Which.Should().Be(_seatId);
    }

    [Fact]
    public void Reserve_WithNullSeatIds_Throw()
    {
        // Arrange
        var action = () =>
            Reservation.Reserve(null!, _customerId, _reservedAt, _reservationDuration);

        // Act + Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Reserve_WithEmptySeatIdsCollection_Throw()
    {
        // Arrange
        var action = () =>
            Reservation.Reserve([], _customerId, _reservedAt, _reservationDuration);

        // Act + Assert
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reserve_WithEmptySeatId_Throw()
    {
        // Arrange
        var action = ()
            => Reservation.Reserve([_emptySeatId], _customerId, _reservedAt, _reservationDuration);

        // Act + Assert
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reserve_WithDuplicateSeatIds_Throw()
    {
        // Arrange
        var action = () =>
            Reservation.Reserve([_seatId, _seatId], _customerId, _reservedAt, _reservationDuration);

        // Act + Assert
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reserve_WithCustomerIdIsEmpty_Throw()
    {
        // Arrange
        var action = ()
            => Reservation.Reserve([_seatId], _emptyCustomerId, _reservedAt, _reservationDuration);

        // Act + Assert
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reserve_WithZeroReservationDuration_Throw()
    {
        // Arrange
        var action = ()
            => Reservation.Reserve([_seatId], _customerId, _reservedAt, _zeroReservationDuration);

        // Act + Assert
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reserve_WithNegativeReservationDuration_Throw()
    {
        // Arrange
        var negativeReservationDuration = TimeSpan.FromMinutes(-1);
        var action = ()
            => Reservation.Reserve([_seatId], _customerId, _reservedAt, negativeReservationDuration);

        // Act + Assert
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Confirm_BeforeExpiration_ConfirmReservation()
    {
        // Arrange
        var reservation =
            Reservation.Reserve([_seatId], _customerId, _reservedAt, _reservationDuration);
        var confirmedAt = _reservedAt.AddMinutes(5);

        // Act
        reservation.Confirm(confirmedAt);

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Confirmed);
        reservation.ConfirmedAt.Should().Be(confirmedAt);
    }

    [Fact]
    public void Confirm_AtExpiration_Throw()
    {
        // Arrange
        var reservation =
            Reservation.Reserve([_seatId], _customerId, _reservedAt, _reservationDuration);
        var action = () => reservation.Confirm(reservation.ExpiresAt);

        // Act + Assert
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Confirm_AfterExpiration_Throw()
    {
        // Arrange
        var reservation =
            Reservation.Reserve([_seatId], _customerId, _reservedAt, _reservationDuration);
        var confirmedAt = reservation.ExpiresAt.AddTicks(1);
        var action = () => reservation.Confirm(confirmedAt);

        // Act + Assert
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_DoNothing()
    {
        // Arrange
        var reservation =
            Reservation.Reserve([_seatId], _customerId, _reservedAt, _reservationDuration);
        var repeatedConfirmedAt = _reservedAt.AddMinutes(10);
        reservation.Confirm(_reservedAt);

        // Act
        reservation.Confirm(repeatedConfirmedAt);

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Confirmed);
        reservation.ConfirmedAt.Should().Be(_reservedAt);
    }

    [Fact]
    public void Expire_BeforeExpiration_Throw()
    {
        // Arrange
        var reservation =
            Reservation.Reserve([_seatId], _customerId, _reservedAt, _reservationDuration);
        var expiredAt = reservation.ExpiresAt.AddTicks(-1);
        var action = () => reservation.Expire(expiredAt);

        // Act + Assert
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Expire_AtExpiration_ExpireReservation()
    {
        // Arrange
        var reservation =
            Reservation.Reserve([_seatId], _customerId, _reservedAt, _reservationDuration);
        var expiredAt = reservation.ExpiresAt;

        // Act
        reservation.Expire(expiredAt);

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Expired);
        reservation.ExpiredAt.Should().Be(expiredAt);
    }

    [Fact]
    public void Expire_WhenReservationIsConfirmed_Throw()
    {
        // Arrange
        var reservation =
            Reservation.Reserve([_seatId], _customerId, _reservedAt, _reservationDuration);
        reservation.Confirm(_reservedAt);
        var action = () => reservation.Expire(reservation.ExpiresAt);

        // Act + Assert
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_BeforeExpiration_CancelReservation()
    {
        // Arrange
        var reservation =
            Reservation.Reserve([_seatId], _customerId, _reservedAt, _reservationDuration);
        var cancelledAt = _reservedAt.AddMinutes(5);

        // Act
        reservation.Cancel(cancelledAt);

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Cancelled);
        reservation.CancelledAt.Should().Be(cancelledAt);
    }

    [Fact]
    public void Cancel_AtExpiration_Throw()
    {
        // Arrange
        var reservation =
            Reservation.Reserve([_seatId], _customerId, _reservedAt, _reservationDuration);
        var action = () => reservation.Cancel(reservation.ExpiresAt);

        // Act + Assert
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_DoNothing()
    {
        // Arrange
        var reservation =
            Reservation.Reserve([_seatId], _customerId, _reservedAt, _reservationDuration);
        var initialCancelledAt = _reservedAt.AddMinutes(5);
        var repeatedCancelledAt = _reservedAt.AddMinutes(10);
        reservation.Cancel(initialCancelledAt);

        // Act
        reservation.Cancel(repeatedCancelledAt);

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Cancelled);
        reservation.CancelledAt.Should().Be(initialCancelledAt);
    }
}
