using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TicketFlow.BuildingBlocks.CQS.Behaviors;

/// <summary>
///     Pipeline behavior responsible for logging.
/// </summary>
/// <typeparam name="TRequest">Type of the request.</typeparam>
/// <typeparam name="TResponse">Type of the response.</typeparam>
public class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull

{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly IHttpContextAccessor _accessor;

    private const string Undefined = "Undefined";

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger, IHttpContextAccessor accessor)
    {
        _logger = logger;
        _accessor = accessor;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var context = _accessor.HttpContext;
        
        var requestName = typeof(TRequest).Name;
        var method = context.Request.Method ?? Undefined;
        var path = context.Request.Path.Value ?? Undefined;

        _logger.LogInformation("Request {RequestName} to {Path} with {Method} started handling",
            requestName,
            path,
            method
        );

        var response = await next(cancellationToken);
        
        _logger.LogInformation("Request {RequestName} to {Path} with {Method} finished handling",
            requestName,
            path,
            method
        );

        return response;
    }
}