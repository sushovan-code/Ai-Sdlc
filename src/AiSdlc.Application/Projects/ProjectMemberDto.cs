namespace AiSdlc.Application.Projects;

public sealed record ProjectMemberDto(Guid UserId, string DisplayName, string Email, string Role, bool IsOwner);
