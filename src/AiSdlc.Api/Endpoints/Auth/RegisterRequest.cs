namespace AiSdlc.Api.Endpoints.Auth;

public sealed record RegisterRequest(string Email, string DisplayName, string Password);
