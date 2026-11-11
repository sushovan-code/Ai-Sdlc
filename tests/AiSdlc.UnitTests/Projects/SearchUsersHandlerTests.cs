using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Application.Projects;
using AiSdlc.Application.UserDirectory.SearchUsers;
using AiSdlc.Domain.Security;
using AiSdlc.UnitTests.Support;
using NSubstitute;
using Shouldly;

namespace AiSdlc.UnitTests.Projects;

public sealed class SearchUsersHandlerTests
{
    private readonly IUserAccounts _accounts = Substitute.For<IUserAccounts>();

    [Fact]
    public async Task Handle_ProjectManager_GetsTrimmedSearchWithTheResultCap()
    {
        var match = TestUsers.User(Guid.NewGuid(), AppRoles.Architect, "Ana Architect");
        _accounts.SearchActiveAsync("ana", SearchUsers.ResultLimit, Arg.Any<CancellationToken>()).Returns([match]);

        var result = await new SearchUsersHandler(TestUsers.Manager(), _accounts)
            .HandleAsync(new SearchUsers("  ana "), CancellationToken.None);

        result.Value.ShouldBe([match]);
    }

    [Theory]
    [InlineData("Architect")]
    [InlineData("Developer")]
    [InlineData("QA")]
    [InlineData("Admin")]
    public async Task Handle_OtherRoles_AreForbidden(string role)
    {
        var result = await new SearchUsersHandler(TestUsers.Caller(Guid.NewGuid(), role), _accounts)
            .HandleAsync(new SearchUsers("a"), CancellationToken.None);

        result.Error.Code.ShouldBe(ProjectErrors.ManagerRequiredCode);
        await _accounts.DidNotReceiveWithAnyArgs().SearchActiveAsync(default, default, default);
    }

    [Fact]
    public async Task Handle_SearchTextTooLong_ReturnsValidationError()
    {
        var result = await new SearchUsersHandler(TestUsers.Manager(), _accounts)
            .HandleAsync(new SearchUsers(new string('x', SearchUsers.MaxQueryLength + 1)), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Validation);
    }
}
