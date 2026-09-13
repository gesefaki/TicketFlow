namespace TicketFlow.BuildingBlocks.CQS.Abstractions.Exceptions.Validation;

/// <summary>
///     Represents a request validation failure with optional errors grouped by field name.
/// </summary>
public class RequestValidationException : Exception
{
    /// <summary>
    /// Represents a request validation failure with optional errors grouped by field name.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }
    
    /// <summary>
    /// Initializes a new instance with a message and no field-level errors.
    /// </summary>
    /// <param name="message">The message describing the validation failure.</param>
    public RequestValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    /// <summary>
    /// Initializes a new instance with a message and field-level validation errors.
    /// </summary>
    /// <param name="message">The message describing the validation failure.</param>
    /// <param name="errors">The validation error messages grouped by field name.</param>
    public RequestValidationException(
        string message,
        IReadOnlyDictionary<string, string[]> errors) : base(message)
    {
        Errors = errors;
    }
}