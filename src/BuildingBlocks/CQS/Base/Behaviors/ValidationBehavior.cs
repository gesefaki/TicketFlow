using MediatR;
using FluentValidation;
using TicketFlow.BuildingBlocks.CQS.Abstractions.Exceptions.Validation;

namespace TicketFlow.BuildingBlocks.CQS.Behaviors;

/// <summary>
///     Pipeline behavior responsible for validation requests.
/// </summary>
/// <param name="validators">Collection of validators.</param>
/// <typeparam name="TRequest">Type of the request.</typeparam>
/// <typeparam name="TResponse">Type of the response.</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> 
    where TRequest : IRequest<TResponse>
{
    private readonly IReadOnlyList<IValidator<TRequest>> _validators =
        validators as IReadOnlyList<IValidator<TRequest>> ?? validators.ToList();

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        if (_validators.Count == 0)
        {
            return await next(cancellationToken);
        }

        var errors = new Dictionary<string, List<string>>();

        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);

            foreach (var failure in result.Errors)
            {
                if (!errors.TryGetValue(failure.PropertyName, out var messages))
                {
                    messages = [];
                    errors.Add(failure.PropertyName, messages);
                }
                
                messages.Add(failure.ErrorMessage);
            }
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(
                "Request validation failed. See validation log for details.",
                errors.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.Distinct().ToArray()));
        }
        
        return await next(cancellationToken);
    }
}