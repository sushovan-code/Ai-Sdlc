using AiSdlc.Domain.Security;
using FluentValidation;

namespace AiSdlc.Application.Users.SetUserRole;

public sealed class SetUserRoleValidator : AbstractValidator<SetUserRole>
{
    public SetUserRoleValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role)
            .Must(AppRoles.IsValid)
            .WithMessage($"Role must be one of: {string.Join(", ", AppRoles.All)}.");
    }
}
