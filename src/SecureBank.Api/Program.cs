using SecureBank.Api.Exceptions;
using SecureBank.Application;
using SecureBank.Infrastructure;
using Microsoft.AspNetCore.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    builder.Configuration);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "SecureBank API v1");
    });
}

await app.Services.InitializeInfrastructureAsync();

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();