using Krizovka.Shared.ErrorHandling.Exceptions;
using Krizovka.Shared.ErrorHandling.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace Krizovka.Shared.ErrorHandling.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var (statusCode, title, detail, errors) = exception switch
        {
            NotFoundException notFound => (404, "Not Found", notFound.Message, null),
            ValidationException validation => (400, "Validation Failed", validation.Message, validation.Errors),
            UnauthorizedException => (401, "Unauthorized", exception.Message, null),
            ForbiddenException => (403, "Forbidden", exception.Message, null),
            ConflictException => (409, "Conflict", exception.Message, null),
            DomainException => (400, "Bad Request", exception.Message, null),
            BadHttpRequestException => (400, "Bad Request", "Malformed request structure", null),
            JsonException => (400, "Bad Request", "Invalid JSON format", null),
            FormatException => (400, "Bad Request", "Data format is invalid", null),
            Exception e when e.GetType().Name == "DbUpdateConcurrencyException" => (409, "Conflict", "The record was modified by another process", null),
            Exception e when e.GetType().FullName == "FluentValidation.ValidationException" => (400, "Validation Failed", e.Message, null),
            _ => (500, "Internal Server Error",
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Testing"
                    ? exception.ToString()
                    : Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                        ? $"{exception.GetType().Name}: {exception.Message}"
                        : "An unexpected error occurred", null)
        };

        // Log the exception
        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception occurred. TraceId: {TraceId}", traceId);
        }
        else
        {
            _logger.LogWarning("Client error occurred: {Message}. TraceId: {TraceId}", exception.Message, traceId);
        }

        var errorResponse = new ErrorResponse
        {
            Type = $"https://tools.ietf.org/html/rfc7231#section-6.{(statusCode / 100)}.{(statusCode % 100)}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path,
            TraceId = traceId,
            Errors = errors
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, options));
    }
}
