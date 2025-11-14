
namespace Application.Common.Exceptions
{
    /// <summary>
    /// Exception thrown when a business rule or data constraint is violated,
    /// resulting in a conflict with the current state of the system.
    /// Maps to HTTP 409 Conflict in the API layer.
    /// Examples include duplicate entities, violated uniqueness constraints,
    /// or concurrent modifications to the same record.
    /// </summary>
    /// <param name="message">A description of the conflict cause.</param>
    public sealed class ConflictException(string message) : Exception(message) { }
}
