namespace AiSdlc.Application.Abstractions;

/// <summary>The signed-in caller, read from the authenticated principal.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    string? Email { get; }

    string? Role { get; }
}
