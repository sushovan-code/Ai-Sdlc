using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Projects;
using AiSdlc.Application.Projects.AddProjectMember;
using AiSdlc.Application.Projects.RemoveProjectMember;
using AiSdlc.Domain.Auditing;
using AiSdlc.Domain.Projects;
using AiSdlc.Domain.Security;
using AiSdlc.UnitTests.Support;
using NSubstitute;
using Shouldly;

namespace AiSdlc.UnitTests.Projects;

public sealed class ProjectMemberHandlerTests
{
    private readonly Project _project = TestProjects.WithTeam();
    private readonly IUserAccounts _accounts = Substitute.For<IUserAccounts>();
    private readonly IAuditLog _audit = Substitute.For<IAuditLog>();
    private readonly UserDto _qa = TestUsers.User(Guid.NewGuid(), AppRoles.QA, "Quinn QA");

    private AddProjectMemberHandler Adder(ICurrentUser caller, IProjectRepository? repository = null) =>
        new(caller, repository ?? TestProjects.RepositoryHolding(_project), _accounts, _audit, new FixedTime(TestProjects.Now));

    private RemoveProjectMemberHandler Remover(ICurrentUser caller, IProjectRepository? repository = null) =>
        new(caller, repository ?? TestProjects.RepositoryHolding(_project), _audit);

    [Fact]
    public async Task Add_ByManager_AddsTheMemberSavesAndAudits()
    {
        _accounts.FindAsync(_qa.Id, Arg.Any<CancellationToken>()).Returns(_qa);
        var repository = TestProjects.RepositoryHolding(_project);

        var result = await Adder(TestUsers.Manager(), repository).HandleAsync(new AddProjectMember(_project.Id, _qa.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Role.ShouldBe(AppRoles.QA);
        result.Value.IsOwner.ShouldBeFalse();
        _project.IsMember(_qa.Id).ShouldBeTrue();
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _audit.Received(1).WriteAsync(
            Arg.Is<AuditRecord>(r => r.Action == AuditActions.ProjectMemberAdded), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Add_ByAMemberWhoIsNotAManager_IsForbidden()
    {
        var result = await Adder(TestUsers.Architect()).HandleAsync(new AddProjectMember(_project.Id, _qa.Id), CancellationToken.None);

        result.Error.Code.ShouldBe(ProjectErrors.ManagerRequiredCode);
    }

    [Fact]
    public async Task Add_ByAManagerWhoIsNotOnTheProject_GetsNotFound()
    {
        var result = await Adder(TestUsers.Outsider()).HandleAsync(new AddProjectMember(_project.Id, _qa.Id), CancellationToken.None);

        result.Error.Code.ShouldBe(ProjectErrors.NotFoundCode);
    }

    [Fact]
    public async Task Add_UnknownOrDeactivatedUser_IsRefused()
    {
        _accounts.FindAsync(_qa.Id, Arg.Any<CancellationToken>()).Returns(_qa with { IsActive = false });

        var deactivated = await Adder(TestUsers.Manager()).HandleAsync(new AddProjectMember(_project.Id, _qa.Id), CancellationToken.None);
        var unknown = await Adder(TestUsers.Manager()).HandleAsync(new AddProjectMember(_project.Id, Guid.NewGuid()), CancellationToken.None);

        deactivated.Error.Code.ShouldBe(ProjectErrors.UserNotFoundCode);
        unknown.Error.Code.ShouldBe(ProjectErrors.UserNotFoundCode);
    }

    [Fact]
    public async Task Add_ExistingMember_ReturnsConflictWithoutSavingOrAuditing()
    {
        var architect = TestUsers.User(TestUsers.ArchitectId, AppRoles.Architect);
        _accounts.FindAsync(architect.Id, Arg.Any<CancellationToken>()).Returns(architect);
        var repository = TestProjects.RepositoryHolding(_project);

        var result = await Adder(TestUsers.Manager(), repository).HandleAsync(new AddProjectMember(_project.Id, architect.Id), CancellationToken.None);

        result.Error.Code.ShouldBe(ProjectErrors.AlreadyMemberCode);
        await repository.DidNotReceiveWithAnyArgs().SaveChangesAsync(CancellationToken.None);
        await _audit.DidNotReceiveWithAnyArgs().WriteAsync(default!, CancellationToken.None);
    }

    [Fact]
    public async Task Remove_Member_RemovesSavesAndAudits()
    {
        var repository = TestProjects.RepositoryHolding(_project);

        var result = await Remover(TestUsers.Manager(), repository)
            .HandleAsync(new RemoveProjectMember(_project.Id, TestUsers.DeveloperId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _project.IsMember(TestUsers.DeveloperId).ShouldBeFalse();
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _audit.Received(1).WriteAsync(
            Arg.Is<AuditRecord>(r => r.Action == AuditActions.ProjectMemberRemoved), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Remove_TheOwner_IsRefused()
    {
        var result = await Remover(TestUsers.Manager())
            .HandleAsync(new RemoveProjectMember(_project.Id, TestUsers.ManagerId), CancellationToken.None);

        result.Error.Code.ShouldBe(ProjectErrors.OwnerCannotBeRemovedCode);
    }

    [Fact]
    public async Task Remove_SomeoneWhoIsNotOnTheProject_ReturnsNotFound()
    {
        var result = await Remover(TestUsers.Manager())
            .HandleAsync(new RemoveProjectMember(_project.Id, Guid.NewGuid()), CancellationToken.None);

        result.Error.Code.ShouldBe(ProjectErrors.NotMemberCode);
    }

    [Fact]
    public async Task Remove_ByANonManager_IsForbidden()
    {
        var result = await Remover(TestUsers.Developer())
            .HandleAsync(new RemoveProjectMember(_project.Id, TestUsers.ArchitectId), CancellationToken.None);

        result.Error.Code.ShouldBe(ProjectErrors.ManagerRequiredCode);
        _project.IsMember(TestUsers.ArchitectId).ShouldBeTrue();
    }
}
