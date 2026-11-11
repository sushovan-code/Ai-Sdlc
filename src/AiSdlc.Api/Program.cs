using AiSdlc.Api.Endpoints.Auth;
using AiSdlc.Api.Endpoints.Users;
using AiSdlc.Api.Security;
using AiSdlc.Application;
using AiSdlc.Infrastructure;

const string ApiV1 = "/api/v1";

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiSecurity();
builder.Services.AddApiRateLimiting(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSecurityHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

var v1 = app.MapGroup(ApiV1);
v1.MapAuthEndpoints();
v1.MapUserEndpoints();

app.Run();

/// <summary>Entry point marker so integration tests can host the API with WebApplicationFactory.</summary>
public partial class Program;
