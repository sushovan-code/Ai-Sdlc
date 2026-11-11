using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Application.Users.SetUserActive;
using AiSdlc.Domain.Auditing;
using NSubstitute;
using Shouldly;

namespace AiSdlc.UnitTests.Users;

public sealed class SetUserActiveHandlerTests
{
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid TargetId = Guid.NewGuid();

    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserAccounts _accounts = Substitute.For<IUserAccounts>();
    private readonly IAuditLog _audit = Substitute.For<IAuditLog>();
    private readonly SetUserActiveHandler _handler;

    public SetUserActiveHandlerTests()
    {
        _currentUser.UserId.Returns(AdminId);
        _handler = new SetUserActiveHandler(_currentUser, _accounts, _audit);
    }

    [Fact]
    public async Task Handle_DeactivatingYourself_ReturnsConflict()
    {
        var result = await _handler.HandleAsync(new SetUserActive(AdminId, false), CancellationToken.None);

        result.Error.Code.ShouldBe(AuthErrors.SelfChangeCode);
        await _accounts.DidNotReceiveWithAnyArgs().SetActiveAsync(default, default, CancellationToken.None);
    }

    [Theory]
    [InlineData(false, AuditActions.UserDeactivated)]
    [InlineData(true, AuditActions.UserActivated)]
    public async Task Handle_ValidChange_AuditsMatchingAction(bool isActive, string expectedAction)
    {
        var updated = new UserDto(TargetId, "dev@demo.local", "Dev", "Developer", isActive);
        _accounts.SetActiveAsync(TargetId, isActive, Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Success(updated));

        var result = await _handler.HandleAsync(new SetUserActive(TargetId, isActive), CancellationToken.None);

        result.Value.IsActive.ShouldBe(isActive);
        await _audit.Received(1).WriteAsync(
            Arg.Is<AuditRecord>(r => r.Action == expectedAction), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        _accounts.SetActiveAsync(TargetId, false, Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Failure(AuthErrors.UserNotFound));

        var result = await _handler.HandleAsync(new SetUserActive(TargetId, false), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }
}
