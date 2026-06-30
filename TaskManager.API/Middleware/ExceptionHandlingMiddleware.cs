using System.Net;
using System.Text.Json;
using FluentValidation;

namespace TaskManager.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        // ── VALIDATION EXCEPTION ───────────────────────
        // Caught BEFORE the generic Exception catch
        // C# checks catch blocks top to bottom — most specific first
        catch (ValidationException validationEx)
        {
            _logger.LogWarning("Validation failed: {Errors}",
                string.Join(", ", validationEx.Errors.Select(e => e.ErrorMessage)));

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 400;

            var errors = validationEx.Errors.Select(e => e.ErrorMessage).ToList();
            var response = new { StatusCode = 400, Errors = errors };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        // ─────────────────────────────────────────────
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new
        {
            StatusCode = context.Response.StatusCode,
            Message = "An unexpected error occurred",
            Details = exception.Message
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}