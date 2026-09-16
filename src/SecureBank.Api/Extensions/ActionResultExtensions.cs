using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace SecureBank.Api.Extensions;

public static class ActionResultExtensions
{
    public static ActionResult<TDestination> ToActionResult<TSource, TDestination>(
        this TSource source)
    {
        return new OkObjectResult(
            source.Adapt<TDestination>());
    }
}
