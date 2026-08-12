using TicketFlow.BuildingBlocks.Domain.Interfaces;

namespace TicketFlow.Reservations.Domain.UnitTests;

internal static class DomainEventTestExtensions
{
    public static IReadOnlyList<IDomainEvent> GetDomainEvents(this IHasDomainEvents eventSource) =>
        eventSource.GetDomainEvents();

    public static void MarkDomainEventsAsDispatched(
        this IHasDomainEvents eventSource,
        IReadOnlyCollection<IDomainEvent> events) =>
        eventSource.MarkDomainEventsAsDispatched(events);
}
