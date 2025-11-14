
namespace Application.Common.Exceptions
{
    /// <summary>
    /// Exception thrown when authentication fails due to invalid credentials
    /// (e.g., incorrect username, password, or refresh token).
    /// Maps to HTTP 401 Unauthorized in the API layer.
    /// Used by authentication services when credential validation does not succeed.
    /// </summary>
    /// <param name="message">A description of the invalid credentials condition.</param>
    public sealed class InvalidCredentialsException(string message) : Exception(message) { }
}
