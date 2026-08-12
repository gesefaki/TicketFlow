using FluentAssertions;
using TicketFlow.BuildingBlocks.Domain.Interfaces;
using TicketFlow.BuildingBlocks.Domain.Models;

namespace TicketFlow.BuildingBlocks.Domain.UnitTests.Models;

public sealed class AggregateRootTests
{
    [Fact]
    public void Raise_WithEvent_AddsEventToPendingEvents()
    {
        var aggregate = new TestAggregate();
        IHasDomainEvents eventSource = aggregate;
        var domainEvent = new TestDomainEvent(1);

        aggregate.RaiseEvent(domainEvent);

        eventSource.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeSameAs(domainEvent);
    }

    [Fact]
    public void Raise_WithNullEvent_ThrowsArgumentNullException()
    {
        var aggregate = new TestAggregate();
        IHasDomainEvents eventSource = aggregate;

        var action = () => aggregate.RaiseEvent(null!);

        action.Should().Throw<ArgumentNullException>();
        eventSource.GetDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public void Raise_WithMultipleEvents_PreservesOrder()
    {
        var aggregate = new TestAggregate();
        IHasDomainEvents eventSource = aggregate;
        var firstEvent = new TestDomainEvent(1);
        var secondEvent = new TestDomainEvent(2);

        aggregate.RaiseEvent(firstEvent);
        aggregate.RaiseEvent(secondEvent);

        eventSource.GetDomainEvents().Should().ContainInOrder(firstEvent, secondEvent);
    }

    [Fact]
    public void GetDomainEvents_WhenNewEventIsRaised_ReturnsStableSnapshot()
    {
        var aggregate = new TestAggregate();
        IHasDomainEvents eventSource = aggregate;
        var firstEvent = new TestDomainEvent(1);
        var secondEvent = new TestDomainEvent(2);
        aggregate.RaiseEvent(firstEvent);

        var snapshot = eventSource.GetDomainEvents();
        aggregate.RaiseEvent(secondEvent);

        snapshot.Should().ContainSingle().Which.Should().BeSameAs(firstEvent);
        eventSource.GetDomainEvents().Should().ContainInOrder(firstEvent, secondEvent);
    }

    [Fact]
    public void GetDomainEvents_ReturnsReadOnlySnapshot()
    {
        var aggregate = new TestAggregate();
        IHasDomainEvents eventSource = aggregate;
        var domainEvent = new TestDomainEvent(1);
        aggregate.RaiseEvent(domainEvent);
        var snapshot = eventSource.GetDomainEvents();
        var collection = snapshot.Should().BeAssignableTo<IList<IDomainEvent>>().Subject;

        var replaceAction = () => collection[0] = new TestDomainEvent(2);
        var addAction = () => collection.Add(new TestDomainEvent(3));

        replaceAction.Should().Throw<NotSupportedException>();
        addAction.Should().Throw<NotSupportedException>();
        eventSource.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeSameAs(domainEvent);
    }

    [Fact]
    public void MarkDomainEventsAsDispatched_WithOneEvent_RemovesOnlyThatEventAndPreservesOrder()
    {
        var aggregate = new TestAggregate();
        IHasDomainEvents eventSource = aggregate;
        var firstEvent = new TestDomainEvent(1);
        var secondEvent = new TestDomainEvent(2);
        var thirdEvent = new TestDomainEvent(3);
        aggregate.RaiseEvent(firstEvent);
        aggregate.RaiseEvent(secondEvent);
        aggregate.RaiseEvent(thirdEvent);

        eventSource.MarkDomainEventsAsDispatched([secondEvent]);

        eventSource.GetDomainEvents().Should().ContainInOrder(firstEvent, thirdEvent);
    }

    [Fact]
    public void MarkDomainEventsAsDispatched_WithEmptyBatch_DoesNothing()
    {
        var aggregate = new TestAggregate();
        IHasDomainEvents eventSource = aggregate;
        var domainEvent = new TestDomainEvent(1);
        aggregate.RaiseEvent(domainEvent);

        eventSource.MarkDomainEventsAsDispatched([]);

        eventSource.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeSameAs(domainEvent);
    }

    [Fact]
    public void MarkDomainEventsAsDispatched_WithNullBatch_ThrowsArgumentNullException()
    {
        var aggregate = new TestAggregate();
        IHasDomainEvents eventSource = aggregate;
        var domainEvent = new TestDomainEvent(1);
        aggregate.RaiseEvent(domainEvent);

        var action = () => eventSource.MarkDomainEventsAsDispatched(null!);

        action.Should().Throw<ArgumentNullException>();
        eventSource.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeSameAs(domainEvent);
    }

    [Fact]
    public void MarkDomainEventsAsDispatched_WithForeignEvent_ThrowsAndPreservesPendingEvents()
    {
        var aggregate = new TestAggregate();
        IHasDomainEvents eventSource = aggregate;
        var pendingEvent = new TestDomainEvent(1);
        aggregate.RaiseEvent(pendingEvent);

        var action = () => eventSource.MarkDomainEventsAsDispatched([new TestDomainEvent(2)]);

        action.Should().Throw<InvalidOperationException>();
        eventSource.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeSameAs(pendingEvent);
    }

    [Fact]
    public void MarkDomainEventsAsDispatched_WhenBatchContainsForeignEvent_DoesNotRemoveAnyEvents()
    {
        var aggregate = new TestAggregate();
        IHasDomainEvents eventSource = aggregate;
        var firstEvent = new TestDomainEvent(1);
        var secondEvent = new TestDomainEvent(2);
        aggregate.RaiseEvent(firstEvent);
        aggregate.RaiseEvent(secondEvent);

        var action = () => eventSource.MarkDomainEventsAsDispatched(
            [firstEvent, new TestDomainEvent(3)]);

        action.Should().Throw<InvalidOperationException>();
        eventSource.GetDomainEvents().Should().ContainInOrder(firstEvent, secondEvent);
    }

    [Fact]
    public void MarkDomainEventsAsDispatched_WithValueEqualEvents_MatchesByReferenceIdentity()
    {
        var aggregate = new TestAggregate();
        IHasDomainEvents eventSource = aggregate;
        var firstEvent = new TestDomainEvent(1);
        var valueEqualEvent = new TestDomainEvent(1);
        aggregate.RaiseEvent(firstEvent);
        aggregate.RaiseEvent(valueEqualEvent);

        eventSource.MarkDomainEventsAsDispatched([valueEqualEvent]);

        eventSource.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeSameAs(firstEvent);
    }

    [Fact]
    public void MarkDomainEventsAsDispatched_WithAlreadyDispatchedEvent_ThrowsInvalidOperationException()
    {
        var aggregate = new TestAggregate();
        IHasDomainEvents eventSource = aggregate;
        var domainEvent = new TestDomainEvent(1);
        aggregate.RaiseEvent(domainEvent);
        eventSource.MarkDomainEventsAsDispatched([domainEvent]);

        var action = () => eventSource.MarkDomainEventsAsDispatched([domainEvent]);

        action.Should().Throw<InvalidOperationException>();
        eventSource.GetDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public void MarkDomainEventsAsDispatched_WithDuplicateReference_DoesNotRemoveAnyEvents()
    {
        var aggregate = new TestAggregate();
        IHasDomainEvents eventSource = aggregate;
        var domainEvent = new TestDomainEvent(1);
        aggregate.RaiseEvent(domainEvent);

        var action = () => eventSource.MarkDomainEventsAsDispatched(
            [domainEvent, domainEvent]);

        action.Should().Throw<InvalidOperationException>();
        eventSource.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeSameAs(domainEvent);
    }

    [Fact]
    public void MarkDomainEventsAsDispatched_DoesNotRemoveEventsRaisedAfterSnapshot()
    {
        var aggregate = new TestAggregate();
        IHasDomainEvents eventSource = aggregate;
        var dispatchedEvent = new TestDomainEvent(1);
        var newlyRaisedEvent = new TestDomainEvent(2);
        aggregate.RaiseEvent(dispatchedEvent);
        var dispatchedSnapshot = eventSource.GetDomainEvents();
        aggregate.RaiseEvent(newlyRaisedEvent);

        eventSource.MarkDomainEventsAsDispatched(dispatchedSnapshot);

        eventSource.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeSameAs(newlyRaisedEvent);
    }

    private sealed class TestAggregate : AggregateRoot<Guid>
    {
        public void RaiseEvent(IDomainEvent domainEvent) => Raise(domainEvent);
    }

    private sealed record TestDomainEvent(int Sequence) : IDomainEvent;
}
