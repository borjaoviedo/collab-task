namespace Api.ErrorHandling
{
    /// <summary>
    /// Centralized URIs for <c>ProblemDetails.type</c> fields used across the API.
    /// Provides consistent problem type identifiers aligned with RFC 7807 for error categorization.
    /// </summary>
    public static class ProblemTypes
    {
        private const string Base = "/problems/";

        public const string ValidationError = Base + "validation-error";
        public const string BadRequest = Base + "bad-request";
        public const string Unauthorized = Base + "unauthorized";
        public const string Forbidden = Base + "forbidden";
        public const string NotFound = Base + "not-found";
        public const string Conflict = Base + "conflict";
        public const string PreconditionFailed = Base + "precondition-failed";
        public const string Unprocessable = Base + "unprocessable-entity";
        public const string RequestTimeout = Base + "request-timeout";
        public const string Internal = Base + "internal-error";
    }
}
