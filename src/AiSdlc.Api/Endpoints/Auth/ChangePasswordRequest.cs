namespace AiSdlc.Api.Endpoints.Auth;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
