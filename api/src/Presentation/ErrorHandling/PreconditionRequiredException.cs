
namespace Api.ErrorHandling
{
    /// <summary>
    /// Exception thrown when a request targets a conditional endpoint
    /// but the required precondition header (e.g. If-Match) is missing.
    /// </summary>
    public sealed class PreconditionRequiredException(string message) : Exception(message) { }
}
