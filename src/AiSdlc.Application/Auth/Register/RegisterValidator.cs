using FluentValidation;

namespace AiSdlc.Application.Auth.Register;

public sealed class RegisterValidator : AbstractValidator<Register>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(AccountLimits.MaxEmailLength);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(AccountLimits.MaxDisplayNameLength);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(AccountLimits.MinPasswordLength)
            .MaximumLength(AccountLimits.MaxPasswordLength);
    }
}
