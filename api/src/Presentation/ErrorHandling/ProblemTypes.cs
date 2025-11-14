
namespace Api.ErrorHandling
{
    /// <summary>
    /// Centralized URIs for <c>ProblemDetails.type</c> values used in the EventDesk API.
    /// 
    /// These identifiers are stable logical names for error categories
    /// (validation, authorization, concurrency, etc.) and are aligned with
    /// the HTTP status codes declared in the API endpoints documentation.
    /// 
    /// By default they are relative URIs (<c>/problems/...</c>). When EventDesk
    /// is deployed under a real domain, they can be replaced with absolute URIs
    /// following RFC 7807 recommendations.
    /// </summary>
    public static class ProblemTypes
    {
        private const string Base = "/problems/";

        /// <summary>
        /// Validation problem for malformed or semantically invalid input.
        /// Typically returned with <c>400 Bad Request</c>.
        /// </summary>
        public const string ValidationError = Base + "validation-error";

        /// <summary>
        /// Generic bad request problem for syntactic or argument errors.
        /// Returned with <c>400 Bad Request</c>.
        /// </summary>
        public const string BadRequest = Base + "bad-request";

        /// <summary>
        /// Authentication failure (missing or invalid credentials).
        /// Returned with <c>401 Unauthorized</c>.
        /// </summary>
        public const string Unauthorized = Base + "unauthorized";

        /// <summary>
        /// Authorization failure (user is authenticated but not allowed).
        /// Returned with <c>403 Forbidden</c>.
        /// </summary>
        public const string Forbidden = Base + "forbidden";

        /// <summary>
        /// Requested resource does not exist or is not visible for the caller.
        /// Returned with <c>404 Not Found</c>.
        /// </summary>
        public const string NotFound = Base + "not-found";

        /// <summary>
        /// Conflict with the current state of the resource (duplicate keys,
        /// optimistic concurrency conflicts, etc.).
        /// Returned with <c>409 Conflict</c>.
        /// </summary>
        public const string Conflict = Base + "conflict";

        /// <summary>
        /// ETag precondition failed (If-Match present but does not match current ETag).
        /// Returned with <c>412 Precondition Failed</c>.
        /// </summary>
        public const string PreconditionFailed = Base + "precondition-failed";

        /// <summary>
        /// ETag precondition required (endpoint requires If-Match header but it is missing).
        /// Returned with <c>428 Precondition Required</c>.
        /// </summary>
        public const string PreconditionRequired = Base + "precondition-required";

        /// <summary>
        /// Generic internal server error for uncaught exceptions.
        /// Returned with <c>500 Internal Server Error</c>.
        /// </summary>
        public const string Internal = Base + "internal-error";
    }
}
