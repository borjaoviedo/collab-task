namespace Application.Abstractions.Auth
{
    /// <summary>
    /// Exposes information about the currently authenticated user within the request scope.
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// Gets the identifier of the current user, or <c>null</c> if no user is authenticated.
        /// </summary>
        Guid? UserId { get; }
    }
}
