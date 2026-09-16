using Mapster;
using Microsoft.AspNetCore.Mvc;
using SecureBank.Application.Abstractions.Models;

namespace SecureBank.Api.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<TValue, TOutput>(
        this Result<TValue> result)
        where TOutput : notnull
    {
        return result.IsSuccess
            ? new OkObjectResult(
                result.Value!.Adapt<TOutput>())
            : new BadRequestObjectResult(
                result.Errors);
    }

    public static IActionResult ToActionResult(
        this Result result)
    {
        return result.IsSuccess
            ? new OkResult()
            : new BadRequestObjectResult(
                result.Errors);
    }
}
