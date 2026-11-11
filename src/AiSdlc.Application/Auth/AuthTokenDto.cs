using AiSdlc.Application.Abstractions;

namespace AiSdlc.Application.Auth;

public sealed record AuthTokenDto(string AccessToken, DateTimeOffset ExpiresAtUtc, UserDto User);
