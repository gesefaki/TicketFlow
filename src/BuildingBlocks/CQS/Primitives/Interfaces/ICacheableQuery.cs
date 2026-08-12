using MediatR;

namespace TicketFlow.BuildingBlocks.CQS.Primitives.Interfaces;

/// <summary>
/// Marks a query whose response can be stored in a cache.
/// </summary>
/// <typeparam name="TResponse">Type of response returned by the query.</typeparam>
public interface ICacheableQuery<out TResponse> : IRequest<TResponse>;
