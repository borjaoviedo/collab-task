
namespace Application.Common.Exceptions
{
    /// <summary>
    /// Exception thrown when a requested entity or resource cannot be found.
    /// Maps to HTTP 404 Not Found in the API layer.
    /// Typical use cases include lookups by identifier or unique key
    /// when the target record does not exist.
    /// </summary>
    /// <param name="message">A human-readable description of the missing entity.</param>
    public sealed class NotFoundException(string message) : Exception(message) { }
}
