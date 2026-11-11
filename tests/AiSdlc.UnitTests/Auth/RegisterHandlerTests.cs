using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Auth.Register;
using AiSdlc.Application.Common;
using AiSdlc.Domain.Auditing;
using AiSdlc.Domain.Security;
using NSubstitute;
using Shouldly;

namespace AiSdlc.UnitTests.Auth;

public sealed class RegisterHandlerTests
{
    private const string StrongPassword = "Str0ng#Passw0rd";

    private readonly IUserAccounts _accounts = Substitute.For<IUserAccounts>();
    private readonly IAuditLog _audit = Substitute.For<IAuditLog>();
    private readonly RegisterHandler _handler;

    public RegisterHandlerTests() => _handler = new RegisterHandler(new RegisterValidator(), _accounts, _audit);

    [Fact]
    public async Task Handle_ValidCommand_AssignsLeastPrivilegeRoleAndAudits()
    {
        var created = new UserDto(Guid.NewGuid(), "new@demo.local", "New User", AppRoles.Developer, true);
        _accounts.RegisterAsync("new@demo.local", "New User", StrongPassword, AppRoles.Developer, Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Success(created));

        var result = await _handler.HandleAsync(
            new Register(" new@demo.local ", " New User ", StrongPassword), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Role.ShouldBe(AppRoles.DefaultSelfRegistrationRole);
        await _audit.Received(1).WriteAsync(
            Arg.Is<AuditRecord>(r => r.Action == AuditActions.UserRegistered),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("not-an-email", "Name", StrongPassword, "Email")]
    [InlineData("a@b.com", "", StrongPassword, "DisplayName")]
    [InlineData("a@b.com", "Name", "short", "Password")]
    public async Task Handle_InvalidInput_ReturnsValidationErrorForThatField(
        string email, string displayName, string password, string field)
    {
        var result = await _handler.HandleAsync(new Register(email, displayName, password), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.ValidationErrors.ShouldNotBeNull().ShouldContainKey(field);
        await _accounts.DidNotReceiveWithAnyArgs().RegisterAsync(default!, default!, default!, default!, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_EmailAlreadyTaken_ReturnsConflictWithoutAuditing()
    {
        var taken = new Error(AuthErrors.EmailTakenCode, "taken", ErrorType.Conflict);
        _accounts.RegisterAsync(default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(Result<UserDto>.Failure(taken));

        var result = await _handler.HandleAsync(
            new Register("dup@demo.local", "Dup", StrongPassword), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Conflict);
        await _audit.DidNotReceiveWithAnyArgs().WriteAsync(default!, CancellationToken.None);
    }
}
