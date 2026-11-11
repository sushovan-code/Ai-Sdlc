using FluentValidation;

namespace AiSdlc.Application.Projects.CreateProject;

public sealed class CreateProjectValidator : AbstractValidator<CreateProject>
{
    public const int MinNameLength = 3;
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 500;

    public CreateProjectValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(MinNameLength).MaximumLength(MaxNameLength);
        RuleFor(x => x.Description).MaximumLength(MaxDescriptionLength);
    }
}
