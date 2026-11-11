// Starts the API and the Angular dev server together. No containers are needed: the database is SQLite.
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.AiSdlc_Api>("api");

// The dev server proxies /api to the API (see src/web/aisdlc-web/proxy.conf.js), so the browser uses one origin.
builder.AddJavaScriptApp("web", "../web/aisdlc-web", "start")
    .WithHttpEndpoint(port: 4200, targetPort: 4200, isProxied: false)
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
