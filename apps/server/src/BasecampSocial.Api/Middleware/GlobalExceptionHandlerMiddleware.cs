using System.Net;
using System.Text.Json;

namespace BasecampSocial.Api.Middleware;

/// <summary>
/// Global exception handling middleware that catches all unhandled exceptions
/// and returns a consistent JSON error response.
///
/// Design decisions:
/// - Catches exceptions at the top of the pipeline so no unhandled error ever
///   returns raw stack traces or HTML error pages to the client.
/// - Returns a consistent JSON envelope: { type, title, status, detail, traceId }
///   following the RFC 7807 Problem Details pattern.
/// - In Development, includes the exception message and stack trace in the response
///   for debugging. In Production, returns a generic message to avoid leaking
///   internal details.
/// - Logs every exception via Serilog with full context (the request path, method,
///   and exception details are captured automatically by Serilog's enrichers).
/// - FluentValidation's ValidationException is handled separately, returning 400
///   with structured error messages per field — perfect for the mobile client to
///   display inline validation errors.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (FluentValidation.ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleValidationExceptionAsync(
        HttpContext context,
        FluentValidation.ValidationException exception)
    {
        _logger.LogWarning(exception, "Validation failed for {Path}", context.Request.Path);

        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            title = "One or more validation errors occurred.",
            status = 400,
            errors,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception,
            "Unhandled exception for {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        var statusCode = exception switch
        {
            ArgumentException => HttpStatusCode.BadRequest,
            KeyNotFoundException => HttpStatusCode.NotFound,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            _ => HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new
        {
            type = statusCode switch
            {
                HttpStatusCode.BadRequest => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                HttpStatusCode.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                HttpStatusCode.Unauthorized => "https://tools.ietf.org/html/rfc7235#section-3.1",
                _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            },
            title = statusCode switch
            {
                HttpStatusCode.BadRequest => "Bad Request",
                HttpStatusCode.NotFound => "Not Found",
                HttpStatusCode.Unauthorized => "Unauthorized",
                _ => "An unexpected error occurred"
            },
            status = (int)statusCode,
            detail = _environment.IsDevelopment()
                ? exception.Message
                : "An error occurred while processing your request.",
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
