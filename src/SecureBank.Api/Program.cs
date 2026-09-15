using SecureBank.Api.Exceptions;
using SecureBank.Application;
using SecureBank.Infrastructure;
using SecureBank.Infrastructure.Documentation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    builder.Configuration);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<
        OAuthSecuritySchemeTransformer>();
});
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/openapi/v1.json",
        "SecureBank API v1");

    options.OAuthClientId("securebank-swagger");
    options.OAuthAppName("SecureBank Swagger");
    options.OAuthScopes(
        "openid",
        "profile",
        "email");

    options.OAuthUsePkce();
});
}

await app.Services.InitializeInfrastructureAsync();

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();