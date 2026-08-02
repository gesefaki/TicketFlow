namespace TicketFlow.BuildingBlocks.Domain.Common;

/// <summary>
/// Default exception for all domain rules violations.
/// </summary>
/// <param name="message">A message conveying information about an exception.</param>
public class DomainException(string message) : Exception(message);