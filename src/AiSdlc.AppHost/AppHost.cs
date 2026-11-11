// Starts the API and the Angular dev server together. No containers are needed: the database is SQLite.
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.AiSdlc_Api>("api");

// Without a key the API uses the offline fake model. With the user secret set, it calls OpenAI instead.
if (!string.IsNullOrWhiteSpace(builder.Configuration["Parameters:openai-api-key"]))
{
    var openAiKey = builder.AddParameter("openai-api-key", secret: true);
    api.WithEnvironment("Ai__Provider", "OpenAI").WithEnvironment("Ai__ApiKey", openAiKey);
}

// The dev server proxies /api to the API (see src/web/aisdlc-web/proxy.conf.js), so the browser uses one origin.
builder.AddJavaScriptApp("web", "../web/aisdlc-web", "start")
    .WithHttpEndpoint(port: 4200, targetPort: 4200, isProxied: false)
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
