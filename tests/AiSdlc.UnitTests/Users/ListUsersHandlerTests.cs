using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Application.Users.ListUsers;
using NSubstitute;
using Shouldly;

namespace AiSdlc.UnitTests.Users;

public sealed class ListUsersHandlerTests
{
    private readonly IUserAccounts _accounts = Substitute.For<IUserAccounts>();
    private readonly ListUsersHandler _handler;

    public ListUsersHandlerTests() => _handler = new ListUsersHandler(new ListUsersValidator(), _accounts);

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, ListUsers.MaxPageSize + 1)]
    public async Task Handle_PageOutOfRange_ReturnsValidationError(int page, int pageSize)
    {
        var result = await _handler.HandleAsync(new ListUsers(page, pageSize), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Validation);
        await _accounts.DidNotReceiveWithAnyArgs().ListAsync(default, default, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ValidPage_ReturnsAccountsPage()
    {
        var page = new PagedResult<UserDto>([new UserDto(Guid.NewGuid(), "a@b.com", "A", "QA", true)], 2, 10, 11);
        _accounts.ListAsync(2, 10, Arg.Any<CancellationToken>()).Returns(page);

        var result = await _handler.HandleAsync(new ListUsers(2, 10), CancellationToken.None);

        result.Value.ShouldBe(page);
    }
}
