using Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using FluentValidation;
using Domain.Common.Exceptions;

namespace Api.ErrorHandling
{
    /// <summary>
    /// Error handling and ProblemDetails configuration for the API layer.
    /// Adds RFC 7807-compliant responses, centralizes exception-to-HTTP mapping,
    /// and enriches payloads with trace identifiers and contextual details.
    /// </summary>
    public static class ErrorHandlingExtensions
    {
        /// <summary>
        /// Registers ProblemDetails and configures a global customizer to include
        /// request <c>instance</c> and <c>traceId</c> in every RFC 7807 response.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddProblemDetailsAndExceptionMapping(this IServiceCollection services)
        {
            services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = ctx =>
                {
                    ctx.ProblemDetails.Instance = ctx.HttpContext.Request.Path;
                    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
                };
            });
            return services;
        }

        /// <summary>
        /// Enables a global exception handler that converts unhandled exceptions into
        /// RFC 7807 <c>ProblemDetails</c> responses.
        /// The mapping is aligned with the non-2xx status codes documented in
        /// the EventDesk API endpoints specification:
        /// <list type="bullet">
        /// <item>400 Bad Request</item>
        /// <item><c>401 Unauthorized</c></item>
        /// <item><c>403 Forbidden</c></item>
        /// <item><c>404 Not Found</c></item>
        /// <item><c>409 Conflict</c></item>
        /// <item><c>412 Precondition Failed</c></item>
        /// <item><c>428 Precondition Required</c></item>
        /// <item><c>500 Internal Server Error</c></item>
        /// </list>
        /// </summary>
        /// <param name="app">The application builder to configure.</param>
        /// <returns>The same <see cref="IApplicationBuilder"/> instance for chaining.</returns>
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(builder =>
            {
                builder.Run(async httpContext =>
                {
                    var feature = httpContext.Features.Get<IExceptionHandlerFeature>();
                    var exception = feature?.Error;

                    var (status, type, title, detail, extensions) = MapException(exception, httpContext);

                    httpContext.Response.StatusCode = status;
                    httpContext.Response.ContentType = "application/problem+json";

                    // TraceId always present
                    extensions ??= new Dictionary<string, object?>();
                    extensions["traceId"] ??= httpContext.TraceIdentifier;

                    var result = Results.Problem(
                        detail: detail,
                        instance: httpContext.Request.Path,
                        statusCode: status,
                        title: title,
                        type: type,
                        extensions: extensions);

                    await result.ExecuteAsync(httpContext);
                });
            });

            return app;
        }

        /// <summary>
        /// Maps a caught <see cref="Exception"/> into a ProblemDetails-compatible shape:
        /// HTTP status code, problem <c>type</c> URI, <c>title</c>, human-readable <c>detail</c>,
        /// and an optional <c>extensions</c> dictionary.
        /// Only the HTTP status codes required by the EventDesk API contract are used:
        /// 400, 401, 403, 404, 409, 412, 428. All other unexpected errors become 500.
        /// </summary>
        /// <param name="exception">The exception to map (can be <c>null</c>).</param>
        /// <param name="httpContext">The current HTTP context.</param>
        /// <returns>
        /// A tuple with:
        /// <list type="bullet">
        ///   <item><description><c>status</c>: HTTP status code.</description></item>
        ///   <item><description><c>type</c>: problem type URI.</description></item>
        ///   <item><description><c>title</c>: short summary of the problem.</description></item>
        ///   <item><description><c>detail</c>: human-readable explanation.</description></item>
        ///   <item><description><c>extensions</c>: extra metadata (e.g. <c>traceId</c>, <c>errors</c>, <c>jsonPath</c>).</description></item>
        /// </list>
        /// </returns>
        private static (
            int status,
            string type,
            string title,
            string detail,
            IDictionary<string, object?> extensions)
        MapException(Exception? exception, HttpContext httpContext)
        {
            var extensions = new Dictionary<string, object?>
            {
                ["traceId"] = httpContext.TraceIdentifier
            };

            switch (exception)
            {
                // -------------------- 400 - Bad Request / Validation --------------------

                case ValidationException validationException:
                    // Thrown by FluentValidation when DTO validation fails
                    {
                        var errors = validationException.Errors
                            .GroupBy(e => e.PropertyName)
                            .ToDictionary(
                                g => g.Key,
                                g => g.Select(e => e.ErrorMessage).ToArray());

                        extensions["errors"] = errors;

                        return (
                            StatusCodes.Status400BadRequest,
                            ProblemTypes.ValidationError,
                            "Validation failed",
                            "One or more validation errors occurred.",
                            extensions);
                    }

                case JsonException jsonException:
                    // Thrown when request body contains malformed JSON
                    {
                        if (!string.IsNullOrWhiteSpace(jsonException.Path))
                        {
                            extensions["jsonPath"] = jsonException.Path;
                        }

                        return (
                            StatusCodes.Status400BadRequest,
                            ProblemTypes.BadRequest,
                            "Malformed JSON payload",
                            "Request body is not valid JSON.",
                            extensions);
                    }

                case BadHttpRequestException badHttpRequest:
                    // Raised by ASP.NET Core for malformed or invalid HTTP requests
                    return (
                        StatusCodes.Status400BadRequest,
                        ProblemTypes.BadRequest,
                        "Bad request",
                        badHttpRequest.Message,
                        extensions);

                case ArgumentException argumentException:
                    // Used for invalid argument in input parsing
                    return (
                        StatusCodes.Status400BadRequest,
                        ProblemTypes.BadRequest,
                        "Bad request",
                        argumentException.Message,
                        extensions);

                case FormatException formatException:
                    // Used for format issues in input parsing
                    return (
                        StatusCodes.Status400BadRequest,
                        ProblemTypes.BadRequest,
                        "Bad request",
                        formatException.Message,
                        extensions);

                // -------------------- 401 - Unauthorized --------------------

                case InvalidCredentialsException invalidCredentials:
                    // Indicates failed authentication due to invalid username, password, or token
                    return (
                        StatusCodes.Status401Unauthorized,
                        ProblemTypes.Unauthorized,
                        "Unauthorized",
                        invalidCredentials.Message,
                        extensions);

                case UnauthorizedAccessException:
                    // Raised when no authentication token is provided or it is invalid
                    return (
                        StatusCodes.Status401Unauthorized,
                        ProblemTypes.Unauthorized,
                        "Unauthorized",
                        "Authentication is required to access this resource.",
                        extensions);

                // -------------------- 403 - Forbidden --------------------

                case ForbiddenAccessException forbidden:
                    // Thrown when user is authenticated but not authorized to access the requested resource
                    return (
                        StatusCodes.Status403Forbidden,
                        ProblemTypes.Forbidden,
                        "Forbidden",
                        forbidden.Message,
                        extensions);

                // -------------------- 404 - Not Found --------------------

                case NotFoundException notFound:
                    // Indicates a requested entity or resource does not exist
                    return (
                        StatusCodes.Status404NotFound,
                        ProblemTypes.NotFound,
                        "Resource not found",
                        notFound.Message,
                        extensions);

                // -------------------- 409 - Conflict --------------------

                case ConflictException conflict:
                    // Thrown when a business rule or data constraint conflict occurs
                    return (
                        StatusCodes.Status409Conflict,
                        ProblemTypes.Conflict,
                        "Conflict",
                        conflict.Message,
                        extensions);

                case DbUpdateConcurrencyException:
                    // Fallback for EF Core concurrency violations not translated by Infrastructure
                    return (
                        StatusCodes.Status409Conflict,
                        ProblemTypes.Conflict,
                        "Conflict",
                        "The resource could not be updated due to a concurrency conflict.",
                        extensions);

                // -------------------- 412 - Precondition Failed --------------------

                case OptimisticConcurrencyException optimistic:
                    // Thrown when an ETag or RowVersion mismatch occurs
                    return (
                        StatusCodes.Status412PreconditionFailed,
                        ProblemTypes.PreconditionFailed,
                        "Precondition failed",
                        optimistic.Message,
                        extensions);

                // -------------------- 428 - Precondition Required --------------------

                case PreconditionRequiredException preconditionRequired:
                    // Indicates a conditional request is missing required If-Match header
                    return (
                        StatusCodes.Status428PreconditionRequired,
                        ProblemTypes.PreconditionRequired,
                        "Precondition required",
                        preconditionRequired.Message,
                        extensions);

                // -------------------- Fallback: 500 - Internal Server Error --------------------

                default:
                    // Generic fallback for unexpected exceptions
                    {
                        var env = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();

                        var detail = env.IsDevelopment() && exception is not null
                            ? exception.Message
                            : "An unexpected error occurred. Please try again or contact support if the problem persists.";

                        return (
                            StatusCodes.Status500InternalServerError,
                            ProblemTypes.Internal,
                            "Unexpected error",
                            detail,
                            extensions);
                    }
            }
        }
    }
}
