using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Auth.ChangePassword;
using AiSdlc.Application.Common;
using AiSdlc.Domain.Auditing;
using NSubstitute;
using Shouldly;

namespace AiSdlc.UnitTests.Auth;

public sealed class ChangePasswordHandlerTests
{
    private const string Current = "Current#Passw0rd";
    private const string Next = "Brand#New1Passw0rd";

    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserAccounts _accounts = Substitute.For<IUserAccounts>();
    private readonly IAuditLog _audit = Substitute.For<IAuditLog>();
    private readonly ChangePasswordHandler _handler;

    public ChangePasswordHandlerTests() =>
        _handler = new ChangePasswordHandler(new ChangePasswordValidator(), _currentUser, _accounts, _audit);

    [Fact]
    public async Task Handle_NotSignedIn_ReturnsUnauthorized()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _handler.HandleAsync(new ChangePassword(Current, Next), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_NewPasswordSameAsCurrent_ReturnsValidationError()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());

        var result = await _handler.HandleAsync(new ChangePassword(Current, Current), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.ValidationErrors.ShouldNotBeNull().ShouldContainKey("NewPassword");
    }

    [Fact]
    public async Task Handle_Success_AuditsPasswordChange()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _currentUser.Email.Returns("me@demo.local");
        _accounts.ChangePasswordAsync(userId, Current, Next, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _handler.HandleAsync(new ChangePassword(Current, Next), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _audit.Received(1).WriteAsync(
            Arg.Is<AuditRecord>(r => r.Action == AuditActions.PasswordChanged && r.Outcome == AuditOutcome.Success),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_AuditsFailureAndReturnsError()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        _accounts.ChangePasswordAsync(default, default!, default!, default)
            .ReturnsForAnyArgs(Result.Failure(Error.Validation("currentPassword", "The current password is incorrect.")));

        var result = await _handler.HandleAsync(new ChangePassword(Current, Next), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await _audit.Received(1).WriteAsync(
            Arg.Is<AuditRecord>(r => r.Outcome == AuditOutcome.Failure),
            Arg.Any<CancellationToken>());
    }
}
