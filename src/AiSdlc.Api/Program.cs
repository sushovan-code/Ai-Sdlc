using System.Text.Json.Serialization;
using AiSdlc.Api.Endpoints.Auth;
using AiSdlc.Api.Endpoints.Directory;
using AiSdlc.Api.Endpoints.Projects;
using AiSdlc.Api.Endpoints.Users;
using AiSdlc.Api.Http;
using AiSdlc.Api.Security;
using AiSdlc.Application;
using AiSdlc.Infrastructure;
using Microsoft.AspNetCore.Http.Features;

const string ApiV1 = "/api/v1";

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiSecurity();
builder.Services.AddApiRateLimiting(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ClientErrorExceptionHandler>();

// Enums travel as strings in every response and request (see docs/standards/naming-conventions.md).
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Multipart uploads are capped just above the largest allowed file; the handler enforces the exact limit.
builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit = ProjectFileEndpoints.UploadRequestLimitBytes);
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
v1.MapProjectEndpoints();
v1.MapProjectFileEndpoints();
v1.MapTeamEndpoints();
v1.MapAnalysisEndpoints();
v1.MapStageEndpoints();
v1.MapWrapupEndpoints();
v1.MapDirectoryEndpoints();

app.Run();

/// <summary>Entry point marker so integration tests can host the API with WebApplicationFactory.</summary>
public partial class Program;
