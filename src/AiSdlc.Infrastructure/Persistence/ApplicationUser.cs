using Microsoft.AspNetCore.Identity;

namespace AiSdlc.Infrastructure.Persistence;

/// <summary>Identity user. Persistence detail: the rest of the app sees <c>UserDto</c>.</summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
