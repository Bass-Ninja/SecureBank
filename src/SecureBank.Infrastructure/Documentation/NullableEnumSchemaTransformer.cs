using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SecureBank.Infrastructure.Documentation;

/// <summary>
/// A nullable enum's schema lists a literal `null` alongside its named
/// values, since that's how JSON Schema expresses nullability for a body
/// property. Query parameters have no way to send that literal, so for
/// an optional enum query param this only offers a value that always
/// fails model binding. Stripping it makes "no value" mean "omit the
/// parameter", which is how nullable query params actually work.
/// </summary>
public sealed class NullableEnumSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (schema.Enum is { Count: > 0 })
        {
            schema.Enum = schema.Enum
                .Where(value => value is not null)
                .ToList();
        }

        return Task.CompletedTask;
    }
}
