using FluentAssertions;
using TicketFlow.BuildingBlocks.Domain.Common;
using TicketFlow.Reservations.Domain.Customers;
using TicketFlow.Reservations.Domain.Events;
using TicketFlow.Reservations.Domain.Reservations;
using TicketFlow.Reservations.Domain.Seats;

namespace TicketFlow.Reservations.Domain.UnitTests;

public class ReservationTests
{
    private readonly SeatId _seatIds = SeatId.New();
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
            Reservation.Reserve([_seatIds], _customerId, _reservedAt, _reservationDuration);

        // Assert
        reservation.Id.Should().NotBe(ReservationId.Empty);

        reservation.SeatIds.Should().ContainSingle()
            .Which.Should().Be(_seatIds);

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
        SeatId[] seatIds = [_seatIds, secondSeatId];

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
        var seatIds = new List<SeatId> { _seatIds };
        var reservation =
            Reservation.Reserve(seatIds, _customerId, _reservedAt, _reservationDuration);

        // Act
        seatIds.Add(SeatId.New());

        // Assert
        reservation.SeatIds.Should().ContainSingle()
            .Which.Should().Be(_seatIds);
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
            Reservation.Reserve([_seatIds, _seatIds], _customerId, _reservedAt, _reservationDuration);

        // Act + Assert
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reserve_WithCustomerIdIsEmpty_Throw()
    {
        // Arrange
        var action = ()
            => Reservation.Reserve([_seatIds], _emptyCustomerId, _reservedAt, _reservationDuration);

        // Act + Assert
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reserve_WithZeroReservationDuration_Throw()
    {
        // Arrange
        var action = ()
            => Reservation.Reserve([_seatIds], _customerId, _reservedAt, _zeroReservationDuration);

        // Act + Assert
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reserve_WithNegativeReservationDuration_Throw()
    {
        // Arrange
        var negativeReservationDuration = TimeSpan.FromMinutes(-1);
        var action = ()
            => Reservation.Reserve([_seatIds], _customerId, _reservedAt, negativeReservationDuration);

        // Act + Assert
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Confirm_BeforeExpiration_ConfirmReservation()
    {
        // Arrange
        var reservation =
            Reservation.Reserve([_seatIds], _customerId, _reservedAt, _reservationDuration);
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
            Reservation.Reserve([_seatIds], _customerId, _reservedAt, _reservationDuration);
        var action = () => reservation.Confirm(reservation.ExpiresAt);

        // Act + Assert
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Confirm_AfterExpiration_Throw()
    {
        // Arrange
        var reservation =
            Reservation.Reserve([_seatIds], _customerId, _reservedAt, _reservationDuration);
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
            Reservation.Reserve([_seatIds], _customerId, _reservedAt, _reservationDuration);
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
            Reservation.Reserve([_seatIds], _customerId, _reservedAt, _reservationDuration);
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
            Reservation.Reserve([_seatIds], _customerId, _reservedAt, _reservationDuration);
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
            Reservation.Reserve([_seatIds], _customerId, _reservedAt, _reservationDuration);
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
            Reservation.Reserve([_seatIds], _customerId, _reservedAt, _reservationDuration);
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
            Reservation.Reserve([_seatIds], _customerId, _reservedAt, _reservationDuration);
        var action = () => reservation.Cancel(reservation.ExpiresAt);

        // Act + Assert
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_DoNothing()
    {
        // Arrange
        var reservation =
            Reservation.Reserve([_seatIds], _customerId, _reservedAt, _reservationDuration);
        var initialCancelledAt = _reservedAt.AddMinutes(5);
        var repeatedCancelledAt = _reservedAt.AddMinutes(10);
        reservation.Cancel(initialCancelledAt);

        // Act
        reservation.Cancel(repeatedCancelledAt);

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Cancelled);
        reservation.CancelledAt.Should().Be(initialCancelledAt);
    }

    [Fact]
    public void Reserve_WithValidValues_RaisesCreatedDomainEvent()
    {
        // Arrange
        var reservation =
            Reservation.Reserve(
                [_seatIds],
                _customerId,
                _reservedAt,
                _reservationDuration
            );

        var domainEvent =
            reservation.GetDomainEvents()
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .BeOfType<ReservationCreatedDomainEvent>()
                .Subject;
        
        // Assert
        domainEvent.ReservationId.Should().Be(reservation.Id);
        domainEvent.CustomerId.Should().Be(_customerId);
        domainEvent.SeatIds.Should().BeEquivalentTo([_seatIds]);
        domainEvent.ReservedAt.Should().Be(_reservedAt);
        domainEvent.ExpiresAt.Should().Be(reservation.ExpiresAt);
    }

    [Fact]
    public void Reserve_WhenCreatedEventSeatIdsAreModified_PreservesEventPayload()
    {
        // Arrange
        var reservation = Reservation.Reserve(
            [_seatIds],
            _customerId,
            _reservedAt,
            _reservationDuration);

        var domainEvent = reservation.GetDomainEvents()
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<ReservationCreatedDomainEvent>()
            .Subject;

        // Act
        var modifiedSeatIds = domainEvent.SeatIds.SetItem(0, SeatId.New());

        // Assert
        domainEvent.SeatIds.Should().ContainSingle()
            .Which.Should().Be(_seatIds);
        modifiedSeatIds.Should().ContainSingle()
            .Which.Should().NotBe(_seatIds);
    }
    
    [Fact]
    public void Confirm_BeforeExpiration_RaisesConfirmedDomainEvent()
    {
        // Arrange
        var reservation = Reservation.Reserve(
            [_seatIds],
            _customerId,
            _reservedAt,
            _reservationDuration);

        reservation.MarkDomainEventsAsDispatched(reservation.GetDomainEvents());

        var confirmedAt = _reservedAt.AddMinutes(5);

        // Act
        reservation.Confirm(confirmedAt);

        // Assert
        var domainEvent = reservation.GetDomainEvents()
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<ReservationConfirmedDomainEvent>()
            .Subject;

        domainEvent.ReservationId.Should().Be(reservation.Id);
        domainEvent.ConfirmedAt.Should().Be(confirmedAt);
    }
    
    [Fact]
    public void Confirm_WhenAlreadyConfirmed_DoesNotRaiseAnotherEvent()
    {
        // Arrange
        var reservation = Reservation.Reserve(
            [_seatIds],
            _customerId,
            _reservedAt,
            _reservationDuration);

        reservation.MarkDomainEventsAsDispatched(reservation.GetDomainEvents());

        // Act
        reservation.Confirm(_reservedAt.AddMinutes(5));
        reservation.MarkDomainEventsAsDispatched(reservation.GetDomainEvents());

        reservation.Confirm(_reservedAt.AddMinutes(10));

        // Assert
        reservation.GetDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public void Expire_AtExpiration_RaisesExpiredDomainEvent()
    {
        // Arrange
        var reservation = Reservation.Reserve(
            [_seatIds],
            _customerId,
            _reservedAt,
            _reservationDuration);

        reservation.MarkDomainEventsAsDispatched(reservation.GetDomainEvents());
        var expiredAt = reservation.ExpiresAt;

        // Act
        reservation.Expire(expiredAt);

        // Assert
        var domainEvent = reservation.GetDomainEvents()
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<ReservationExpiredDomainEvent>()
            .Subject;

        domainEvent.ReservationId.Should().Be(reservation.Id);
        domainEvent.ExpiredAt.Should().Be(expiredAt);
    }

    [Fact]
    public void Expire_WhenAlreadyExpired_DoesNotRaiseAnotherEvent()
    {
        // Arrange
        var reservation = Reservation.Reserve(
            [_seatIds],
            _customerId,
            _reservedAt,
            _reservationDuration);

        reservation.Expire(reservation.ExpiresAt);
        reservation.MarkDomainEventsAsDispatched(reservation.GetDomainEvents());

        // Act
        reservation.Expire(reservation.ExpiresAt.AddMinutes(1));

        // Assert
        reservation.GetDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public void Cancel_BeforeExpiration_RaisesCancelledDomainEvent()
    {
        // Arrange
        var reservation = Reservation.Reserve(
            [_seatIds],
            _customerId,
            _reservedAt,
            _reservationDuration);

        reservation.MarkDomainEventsAsDispatched(reservation.GetDomainEvents());
        var cancelledAt = _reservedAt.AddMinutes(5);

        // Act
        reservation.Cancel(cancelledAt);

        // Assert
        var domainEvent = reservation.GetDomainEvents()
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<ReservationCancelledDomainEvent>()
            .Subject;

        domainEvent.ReservationId.Should().Be(reservation.Id);
        domainEvent.CancelledAt.Should().Be(cancelledAt);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_DoesNotRaiseAnotherEvent()
    {
        // Arrange
        var reservation = Reservation.Reserve(
            [_seatIds],
            _customerId,
            _reservedAt,
            _reservationDuration);

        reservation.Cancel(_reservedAt.AddMinutes(5));
        reservation.MarkDomainEventsAsDispatched(reservation.GetDomainEvents());

        // Act
        reservation.Cancel(_reservedAt.AddMinutes(10));

        // Assert
        reservation.GetDomainEvents().Should().BeEmpty();
    }
}
