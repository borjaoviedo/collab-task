using Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using FluentValidation;
using Domain.Common.Exceptions;

namespace Api.Errors
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
        /// Enables a global exception handler that converts thrown exceptions into
        /// RFC 7807 ProblemDetails using a consistent mapping strategy.
        /// Ensures <c>application/problem+json</c> content type and appropriate status codes.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The same application builder for chaining.</returns>
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(builder =>
            {
                builder.Run(async http =>
                {
                    var feature = http.Features.Get<IExceptionHandlerFeature>();
                    var ex = feature?.Error;

                    var (status, type, title, detail, extensions) = MapException(ex, http);

                    // Ensures content-type RFC7807
                    http.Response.ContentType = "application/problem+json";
                    http.Response.StatusCode = status;

                    var result = Results.Problem(
                        detail: detail,
                        instance: http.Request.Path,
                        statusCode: status,
                        title: title,
                        type: type,
                        extensions: extensions);

                    await result.ExecuteAsync(http);
                });
            });
            return app;
        }

        /// <summary>
        /// Maps a caught <see cref="Exception"/> to a ProblemDetails-compatible tuple,
        /// selecting status code, problem <c>type</c>, <c>title</c>, human-readable <c>detail</c>,
        /// and an <c>extensions</c> dictionary (e.g., <c>traceId</c>, validation errors, JSON path).
        /// </summary>
        /// <param name="ex">The exception to map (can be <c>null</c>).</param>
        /// <param name="http">The current HTTP context for request/host information.</param>
        /// <returns>
        /// A tuple with:
        /// <list type="bullet">
        /// <item><description><c>status</c>: HTTP status code.</description></item>
        /// <item><description><c>type</c>: problem type URI.</description></item>
        /// <item><description><c>title</c>: short summary of the problem.</description></item>
        /// <item><description><c>detail</c>: human-readable explanation.</description></item>
        /// <item><description><c>extensions</c>: extra properties (e.g., <c>traceId</c>, <c>errors</c>).</description></item>
        /// </list>
        /// </returns>
        private static (
            int status,
            string type,
            string title,
            string detail,
            IDictionary<string, object?> extensions)
        MapException(Exception? ex, HttpContext http)
        {
            // Base
            var extensions = new Dictionary<string, object?>
            {
                ["traceId"] = http.TraceIdentifier
            };

            switch (ex)
            {
                // 400 - Bad Request / Malformed Payload
                case BadHttpRequestException bhe:
                    return (
                        StatusCodes.Status400BadRequest,
                        ProblemTypes.BadRequest,
                        "Bad request",
                        bhe.Message,
                        extensions);

                case JsonException je:
                    if (!string.IsNullOrWhiteSpace(je.Path)) extensions["jsonPath"] = je.Path;
                    return (
                        StatusCodes.Status400BadRequest,
                        ProblemTypes.BadRequest,
                        "Malformed JSON",
                        "Request body is not valid JSON.",
                        extensions);

                case ArgumentException ae:
                    return (
                        StatusCodes.Status400BadRequest,
                        ProblemTypes.BadRequest,
                        "Bad request",
                        ae.Message,
                        extensions);

                // 400 - Validation
                case ValidationException ve:
                    var errors = ve.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                    extensions["errors"] = errors;
                    return (
                        StatusCodes.Status400BadRequest,
                        ProblemTypes.ValidationError,
                        "Validation failed",
                        "One or more validation errors occurred.",
                        extensions);

                // 401/403 - Unauthorized
                case UnauthorizedAccessException:
                    return (
                        StatusCodes.Status401Unauthorized,
                        ProblemTypes.Unauthorized,
                        "Unauthorized",
                        "Authentication required.",
                        extensions);

                case InvalidCredentialsException ice:
                    return (
                        StatusCodes.Status401Unauthorized,
                        ProblemTypes.Unauthorized,
                        "Unauthorized",
                        ice.Message,
                        extensions);

                case ForbiddenAccessException fae:
                    return (
                        StatusCodes.Status403Forbidden,
                        ProblemTypes.Forbidden,
                        "Forbidden",
                        fae.Message,
                        extensions);

                // 404 - Not Found
                case NotFoundException nfe:
                    return (
                        StatusCodes.Status404NotFound,
                        ProblemTypes.NotFound,
                        "Resource not found",
                        nfe.Message,
                        extensions);

                // 409 - Conflict
                case DuplicateEntityException dee:
                    return (
                        StatusCodes.Status409Conflict,
                        ProblemTypes.Conflict,
                        "Conflict",
                        dee.Message,
                        extensions);

                case DbUpdateConcurrencyException:
                    return (
                        StatusCodes.Status409Conflict,
                        ProblemTypes.Conflict,
                        "Concurrency conflict",
                        "The resource was modified by another process.",
                        extensions);

                // 412 - If-Match / ETag
                case PreconditionFailedException pfe:
                    return (
                        StatusCodes.Status412PreconditionFailed,
                        ProblemTypes.PreconditionFailed,
                        "Precondition failed",
                        pfe.Message,
                        extensions);

                // 422 - Domain Rules
                case DomainRuleViolationException dr:
                    return (
                        StatusCodes.Status422UnprocessableEntity,
                        ProblemTypes.Unprocessable,
                        "Unprocessable entity",
                        dr.Message,
                        extensions);

                // 408 - Cancellation / Timeout
                case OperationCanceledException:
                    return (
                        StatusCodes.Status408RequestTimeout,
                        ProblemTypes.RequestTimeout,
                        "Request timeout",
                        "The request was canceled or timed out.",
                        extensions);

                // 500 - Generic
                default:
                    var isDev = http.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment();
                    var safe = isDev && ex is not null ? ex.Message : "An unexpected error occurred.";
                    return (
                        StatusCodes.Status500InternalServerError,
                        ProblemTypes.Internal,
                        "Unexpected error",
                        safe,
                        extensions);
            }
        }
    }
}
