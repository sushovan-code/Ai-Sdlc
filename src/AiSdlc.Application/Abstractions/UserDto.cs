namespace AiSdlc.Application.Abstractions;

public sealed record UserDto(Guid Id, string Email, string DisplayName, string Role, bool IsActive);
