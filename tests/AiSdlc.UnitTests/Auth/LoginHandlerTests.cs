using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Auth.Login;
using AiSdlc.Application.Common;
using AiSdlc.Domain.Auditing;
using NSubstitute;
using Shouldly;

namespace AiSdlc.UnitTests.Auth;

public sealed class LoginHandlerTests
{
    private static readonly UserDto Priya = new(Guid.NewGuid(), "priya@demo.local", "Priya", "Developer", true);
    private static readonly AccessToken Token = new("jwt", DateTimeOffset.UnixEpoch);

    private readonly IUserAccounts _accounts = Substitute.For<IUserAccounts>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly IAuditLog _audit = Substitute.For<IAuditLog>();
    private readonly LoginHandler _handler;

    public LoginHandlerTests() =>
        _handler = new LoginHandler(new LoginValidator(), _accounts, _tokens, _audit);

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokenAndAuditsSuccess()
    {
        _accounts.VerifyCredentialsAsync(Priya.Email, "pw", Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Success(Priya));
        _tokens.Issue(Priya).Returns(Token);

        var result = await _handler.HandleAsync(new Login(Priya.Email, "pw"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldBe("jwt");
        result.Value.User.ShouldBe(Priya);
        await _audit.Received(1).WriteAsync(
            Arg.Is<AuditRecord>(r => r.Action == AuditActions.LoginSucceeded && r.ActorId == Priya.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsGenericUnauthorizedAndAuditsFailure()
    {
        _accounts.VerifyCredentialsAsync(Priya.Email, "bad", Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Failure(AuthErrors.InvalidCredentials));

        var result = await _handler.HandleAsync(new Login(Priya.Email, "bad"), CancellationToken.None);

        result.Error.Code.ShouldBe(AuthErrors.InvalidCredentialsCode);
        result.Error.Type.ShouldBe(ErrorType.Unauthorized);
        _tokens.DidNotReceiveWithAnyArgs().Issue(default!);
        await _audit.Received(1).WriteAsync(
            Arg.Is<AuditRecord>(r => r.Action == AuditActions.LoginFailed && r.Outcome == AuditOutcome.Failure),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DisabledAccount_ReturnsSameErrorAsWrongPassword()
    {
        var disabled = new Error(AuthErrors.AccountDisabledCode, "Account is disabled.", ErrorType.Unauthorized);
        _accounts.VerifyCredentialsAsync(Priya.Email, "pw", Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Failure(disabled));

        var result = await _handler.HandleAsync(new Login(Priya.Email, "pw"), CancellationToken.None);

        result.Error.ShouldBe(AuthErrors.InvalidCredentials);
        await _audit.Received(1).WriteAsync(
            Arg.Is<AuditRecord>(r => r.Details == AuthErrors.AccountDisabledCode),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LockedOut_ReturnsLockedOutAndAuditsLockout()
    {
        _accounts.VerifyCredentialsAsync(Priya.Email, "pw", Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Failure(AuthErrors.LockedOut));

        var result = await _handler.HandleAsync(new Login(Priya.Email, "pw"), CancellationToken.None);

        result.Error.Code.ShouldBe(AuthErrors.LockedOutCode);
        await _audit.Received(1).WriteAsync(
            Arg.Is<AuditRecord>(r => r.Action == AuditActions.UserLockedOut),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyEmail_ReturnsValidationErrorWithoutCheckingCredentials()
    {
        var result = await _handler.HandleAsync(new Login(string.Empty, "pw"), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.ValidationErrors.ShouldNotBeNull().ShouldContainKey("Email");
        await _accounts.DidNotReceiveWithAnyArgs()
            .VerifyCredentialsAsync(default!, default!, CancellationToken.None);
    }
}
