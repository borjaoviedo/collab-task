
namespace Application.Common.Exceptions
{
    /// <summary>
    /// Exception thrown when a concurrency token (such as a RowVersion or ETag)
    /// does not match the current persisted value, indicating that another process
    /// has modified the resource in the meantime.
    /// Maps to HTTP 412 Precondition Failed in the API layer.
    /// Typically thrown by repositories or Unit of Work implementations
    /// after catching <c>DbUpdateConcurrencyException</c>.
    /// </summary>
    /// <param name="message">A human-readable explanation of the concurrency issue.</param>
    public sealed class OptimisticConcurrencyException(string message) : Exception(message) { }
}
