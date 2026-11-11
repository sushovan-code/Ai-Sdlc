using FluentValidation;

namespace AiSdlc.Application.Users.ListUsers;

public sealed class ListUsersValidator : AbstractValidator<ListUsers>
{
    public ListUsersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(ListUsers.FirstPage);
        RuleFor(x => x.PageSize).InclusiveBetween(1, ListUsers.MaxPageSize);
    }
}
