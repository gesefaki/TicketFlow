using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TicketFlow.BuildingBlocks.CQS.Behaviors;

/// <summary>
///     Pipeline behavior responsible for performance tracking.
/// </summary>
/// <param name="logger">The logger instance.</param>
/// <typeparam name="TRequest">Type of the request.</typeparam>
/// <typeparam name="TResponse">Type of the response.</typeparam>
public sealed class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const int WarningThreshold = 700;

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        var stopwatch = Stopwatch.StartNew();
        
        var response = await next(cancellationToken);
        
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > WarningThreshold)
        {
            logger.LogWarning("Request {RequestName} took {StopwatchElapsedMilliseconds} milliseconds",
                requestName, 
                stopwatch.ElapsedMilliseconds);
        }

        return response;
    }

}