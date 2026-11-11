using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Application.Users.SetUserRole;
using AiSdlc.Domain.Auditing;
using AiSdlc.Domain.Security;
using NSubstitute;
using Shouldly;

namespace AiSdlc.UnitTests.Users;

public sealed class SetUserRoleHandlerTests
{
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid TargetId = Guid.NewGuid();

    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserAccounts _accounts = Substitute.For<IUserAccounts>();
    private readonly IAuditLog _audit = Substitute.For<IAuditLog>();
    private readonly SetUserRoleHandler _handler;

    public SetUserRoleHandlerTests()
    {
        _currentUser.UserId.Returns(AdminId);
        _currentUser.Email.Returns("admin@demo.local");
        _handler = new SetUserRoleHandler(new SetUserRoleValidator(), _currentUser, _accounts, _audit);
    }

    [Fact]
    public async Task Handle_UnknownRole_ReturnsValidationError()
    {
        var result = await _handler.HandleAsync(new SetUserRole(TargetId, "Superuser"), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Validation);
        await _accounts.DidNotReceiveWithAnyArgs().SetRoleAsync(default, default!, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ChangingOwnRole_ReturnsConflict()
    {
        var result = await _handler.HandleAsync(new SetUserRole(AdminId, AppRoles.Developer), CancellationToken.None);

        result.Error.Code.ShouldBe(AuthErrors.SelfChangeCode);
        await _accounts.DidNotReceiveWithAnyArgs().SetRoleAsync(default, default!, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFoundWithoutAuditing()
    {
        _accounts.SetRoleAsync(TargetId, AppRoles.QA, Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Failure(AuthErrors.UserNotFound));

        var result = await _handler.HandleAsync(new SetUserRole(TargetId, AppRoles.QA), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        await _audit.DidNotReceiveWithAnyArgs().WriteAsync(default!, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ValidChange_AuditsWhoChangedWhom()
    {
        var updated = new UserDto(TargetId, "qa@demo.local", "QA", AppRoles.QA, true);
        _accounts.SetRoleAsync(TargetId, AppRoles.QA, Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Success(updated));

        var result = await _handler.HandleAsync(new SetUserRole(TargetId, AppRoles.QA), CancellationToken.None);

        result.Value.Role.ShouldBe(AppRoles.QA);
        await _audit.Received(1).WriteAsync(
            Arg.Is<AuditRecord>(r =>
                r.Action == AuditActions.UserRoleChanged && r.ActorId == AdminId && r.Details == "qa@demo.local -> QA"),
            Arg.Any<CancellationToken>());
    }
}
