using MediatR;

namespace TicketFlow.BuildingBlocks.CQS.Primitives.Interfaces;

public interface ICacheableQuery<out TResponse> : IRequest<TResponse>;