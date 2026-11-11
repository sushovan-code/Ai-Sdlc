using AiSdlc.Application.Abstractions;
using AiSdlc.Domain.Security;
using NSubstitute;

namespace AiSdlc.UnitTests.Support;

public static class TestUsers
{
    public static readonly Guid ManagerId = Guid.NewGuid();
    public static readonly Guid ArchitectId = Guid.NewGuid();
    public static readonly Guid DeveloperId = Guid.NewGuid();
    public static readonly Guid OutsiderId = Guid.NewGuid();

    public static UserDto User(Guid id, string role, string name = "Someone", bool isActive = true) =>
        new(id, $"{name.ToLowerInvariant().Replace(' ', '.')}@demo.local", name, role, isActive);

    public static ICurrentUser Caller(Guid id, string role)
    {
        var caller = Substitute.For<ICurrentUser>();
        caller.IsAuthenticated.Returns(true);
        caller.UserId.Returns(id);
        caller.Email.Returns($"{role.ToLowerInvariant()}@demo.local");
        caller.Role.Returns(role);
        return caller;
    }

    public static ICurrentUser Manager() => Caller(ManagerId, AppRoles.ProjectManager);

    public static ICurrentUser Architect() => Caller(ArchitectId, AppRoles.Architect);

    public static ICurrentUser Developer() => Caller(DeveloperId, AppRoles.Developer);

    public static ICurrentUser Outsider() => Caller(OutsiderId, AppRoles.ProjectManager);
}
