namespace AiSdlc.Api.Security;

public static class SecurityHeadersExtensions
{
    /// <summary>
    /// Adds baseline response headers. The API serves JSON only, so the content policy forbids everything.
    /// HSTS is added by <c>UseHsts</c> outside development.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
            await next();
        });
}
