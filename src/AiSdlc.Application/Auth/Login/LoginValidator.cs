using FluentValidation;

namespace AiSdlc.Application.Auth.Login;

public sealed class LoginValidator : AbstractValidator<Login>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(AccountLimits.MaxEmailLength);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(AccountLimits.MaxPasswordLength);
    }
}
