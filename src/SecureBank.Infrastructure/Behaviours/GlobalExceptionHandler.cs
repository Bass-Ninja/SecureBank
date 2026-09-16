using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SecureBank.Application.Exceptions;

namespace SecureBank.Infrastructure.Behaviours;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "An unhandled exception occurred.");

        var statusCode = exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            ForbiddenException => StatusCodes.Status403Forbidden,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode == StatusCodes.Status400BadRequest
                ? "Request failed."
                : "An unexpected error occurred.",
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? null
                : exception.Message,
            Instance = httpContext.Request.Path
        };

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["errors"] =
                validationException.Errors
                    .GroupBy(error => error.PropertyName)
                    .ToDictionary(
                        group => group.Key,
                        group => group
                            .Select(error => error.ErrorMessage)
                            .ToArray());
        }

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }
}
