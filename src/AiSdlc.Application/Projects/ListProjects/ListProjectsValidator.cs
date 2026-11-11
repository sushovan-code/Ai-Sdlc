using FluentValidation;

namespace AiSdlc.Application.Projects.ListProjects;

public sealed class ListProjectsValidator : AbstractValidator<ListProjects>
{
    public ListProjectsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(ListProjects.FirstPage);
        RuleFor(x => x.PageSize).InclusiveBetween(1, ListProjects.MaxPageSize);
    }
}
