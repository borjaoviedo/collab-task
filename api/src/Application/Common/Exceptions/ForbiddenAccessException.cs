
namespace Application.Common.Exceptions
{
    /// <summary>
    /// Exception thrown when an authenticated user attempts to perform an operation
    /// that they do not have permission to execute.
    /// Maps to HTTP 403 Forbidden in the API layer.
    /// Used for authorization checks that cannot be expressed solely through
    /// declarative authorization policies.
    /// </summary>
    /// <param name="message">A description of the authorization failure.</param>
    public sealed class ForbiddenAccessException(string message) : Exception(message) { }
}
