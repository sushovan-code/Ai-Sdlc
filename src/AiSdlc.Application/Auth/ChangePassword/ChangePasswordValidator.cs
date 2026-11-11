using FluentValidation;

namespace AiSdlc.Application.Auth.ChangePassword;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePassword>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().MaximumLength(AccountLimits.MaxPasswordLength);
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(AccountLimits.MinPasswordLength)
            .MaximumLength(AccountLimits.MaxPasswordLength)
            .NotEqual(x => x.CurrentPassword).WithMessage("The new password must differ from the current one.");
    }
}
