using AiSdlc.Domain.Security;
using Shouldly;

namespace AiSdlc.UnitTests.Domain;

public sealed class AppRolesTests
{
    [Fact]
    public void All_ContainsTheFiveRolesOfTheUseCase()
    {
        AppRoles.All.ShouldBe(["ProjectManager", "Architect", "Developer", "QA", "Admin"], ignoreOrder: true);
    }

    [Fact]
    public void DefaultSelfRegistrationRole_IsNotAPrivilegedRole()
    {
        AppRoles.DefaultSelfRegistrationRole.ShouldBe(AppRoles.Developer);
        AppRoles.DefaultSelfRegistrationRole.ShouldNotBe(AppRoles.Admin);
    }

    [Theory]
    [InlineData("QA", true)]
    [InlineData("qa", false)]
    [InlineData("Superuser", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_ChecksExactRoleName(string? role, bool expected)
    {
        AppRoles.IsValid(role).ShouldBe(expected);
    }
}
