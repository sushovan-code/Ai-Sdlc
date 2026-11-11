using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Projects;
using AiSdlc.Application.Projects.GetProject;
using AiSdlc.Domain.Projects;
using AiSdlc.Domain.Security;
using AiSdlc.UnitTests.Support;
using NSubstitute;
using Shouldly;

namespace AiSdlc.UnitTests.Projects;

public sealed class GetProjectHandlerTests
{
    private readonly Project _project = TestProjects.WithTeam();
    private readonly IUserAccounts _accounts = Substitute.For<IUserAccounts>();

    public GetProjectHandlerTests()
    {
        _accounts.FindManyAsync(default!, default).ReturnsForAnyArgs(
        [
            TestUsers.User(TestUsers.ManagerId, AppRoles.ProjectManager, "Pat Manager"),
            TestUsers.User(TestUsers.ArchitectId, AppRoles.Architect, "Ana Architect"),
            TestUsers.User(TestUsers.DeveloperId, AppRoles.Developer, "Dev Developer"),
        ]);
    }

    private GetProjectHandler HandlerFor(ICurrentUser caller) =>
        new(caller, TestProjects.RepositoryHolding(_project), _accounts);

    [Fact]
    public async Task Handle_Member_SeesTheProjectWithItsTeamOwnerFirst()
    {
        var result = await HandlerFor(TestUsers.Architect()).HandleAsync(new GetProject(_project.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Members.Select(m => m.DisplayName).ShouldBe(["Pat Manager", "Ana Architect", "Dev Developer"]);
        result.Value.Members[0].IsOwner.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_NonMember_GetsNotFoundSoTheProjectCannotBeProbed()
    {
        var result = await HandlerFor(TestUsers.Outsider()).HandleAsync(new GetProject(_project.Id), CancellationToken.None);

        result.Error.Code.ShouldBe(ProjectErrors.NotFoundCode);
    }

    [Fact]
    public async Task Handle_UnknownProject_GetsTheSameNotFound()
    {
        var result = await HandlerFor(TestUsers.Manager()).HandleAsync(new GetProject(Guid.NewGuid()), CancellationToken.None);

        result.Error.Code.ShouldBe(ProjectErrors.NotFoundCode);
    }

    [Fact]
    public async Task Handle_CanManageIsTrueOnlyForProjectManagers()
    {
        var asManager = await HandlerFor(TestUsers.Manager()).HandleAsync(new GetProject(_project.Id), CancellationToken.None);
        var asDeveloper = await HandlerFor(TestUsers.Developer()).HandleAsync(new GetProject(_project.Id), CancellationToken.None);

        asManager.Value.CanManage.ShouldBeTrue();
        asDeveloper.Value.CanManage.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ListsOnlyLiveFilesAndFlagsTheLatestVersion()
    {
        TestProjects.AddRequirements(_project, 2);
        var third = TestProjects.AddRequirements(_project);
        _project.DeleteFile(third.Id, TestUsers.ManagerId, TestProjects.Now);

        var result = await HandlerFor(TestUsers.Manager()).HandleAsync(new GetProject(_project.Id), CancellationToken.None);

        result.Value.Files.Select(f => (f.Version, f.IsLatest)).ShouldBe([(2, true), (1, false)]);
        result.Value.Files.ShouldAllBe(f => f.UploadedByName == "Pat Manager");
    }
}
