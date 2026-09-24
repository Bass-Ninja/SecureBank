namespace SecureBank.Api;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers.Append("Referrer-Policy", "no-referrer");
            headers.Append(
                "Permissions-Policy",
                "camera=(), geolocation=(), microphone=()");

            if (!context.Request.Path.StartsWithSegments("/swagger")
                && !context.Request.Path.StartsWithSegments("/openapi"))
            {
                headers.Append(
                    "Content-Security-Policy",
                    "default-src 'none'; frame-ancestors 'none'");
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(
        this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
