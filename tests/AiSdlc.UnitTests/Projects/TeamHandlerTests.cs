using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Projects;
using AiSdlc.Application.Team.GetTeam;
using AiSdlc.Application.Team.SetSprintCadence;
using AiSdlc.Domain.Auditing;
using AiSdlc.Domain.Projects;
using AiSdlc.UnitTests.Support;
using NSubstitute;
using Shouldly;

namespace AiSdlc.UnitTests.Projects;

public sealed class TeamHandlerTests
{
    private readonly Project _project = TestProjects.WithTeam();
    private readonly IAuditLog _audit = Substitute.For<IAuditLog>();

    private void UploadTeam()
    {
        var file = _project.AddFile(FileKind.TeamTemplate, "team.xlsx", 10, new string('D', 64), "k/t", TestUsers.ManagerId, TestProjects.Now);
        _project.AttachTeam(file,
        [
            new TeamMemberDraft("Ravi Kumar", "Developer", ["Angular", "TypeScript"], 40, 40),
            new TeamMemberDraft("Asha Rao", "Developer", ["C#", "SQL"], 40, 30),
        ]);
    }

    private GetTeamHandler Getter(ICurrentUser caller) => new(caller, TestProjects.RepositoryHolding(_project));

    private SetSprintCadenceHandler Setter(ICurrentUser caller, IProjectRepository? repository = null) =>
        new(caller, repository ?? TestProjects.RepositoryHolding(_project), _audit);

    [Fact]
    public async Task GetTeam_ListsPeopleByNameWithTheirSprintCapacityAndNothingBookedYet()
    {
        UploadTeam();

        var result = await Getter(TestUsers.Developer()).HandleAsync(new GetTeam(_project.Id), CancellationToken.None);

        result.Value.Members.Select(m => m.Name).ShouldBe(["Asha Rao", "Ravi Kumar"]);
        var asha = result.Value.Members[0];
        asha.SprintCapacityHours.ShouldBe(30);
        asha.AllocatedHours.ShouldBe(0);
        asha.TotalCapacityHours.ShouldBe(30);
        asha.UtilizationPercent.ShouldBe(0);
        result.Value.TotalCapacityHours.ShouldBe(70);
        result.Value.HoursPerDay.ShouldBe(8);
    }

    [Theory]
    [InlineData(2, 30)]
    [InlineData(3, 45)]
    [InlineData(4, 60)]
    public async Task GetTeam_ScalesCapacityToTheProjectsSprintLength(int weeks, decimal ashaSprintHours)
    {
        UploadTeam();
        _project.SetSprintCadence(weeks);

        var result = await Getter(TestUsers.Manager()).HandleAsync(new GetTeam(_project.Id), CancellationToken.None);

        result.Value.SprintWeeks.ShouldBe(weeks);
        result.Value.Members.Single(m => m.Name == "Asha Rao").SprintCapacityHours.ShouldBe(ashaSprintHours);
    }

    [Fact]
    public async Task GetTeam_WithoutATeamSheet_IsEmptyNotAnError()
    {
        var result = await Getter(TestUsers.Manager()).HandleAsync(new GetTeam(_project.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Members.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetTeam_ByANonMember_GetsNotFound()
    {
        var result = await Getter(TestUsers.Outsider()).HandleAsync(new GetTeam(_project.Id), CancellationToken.None);

        result.Error.Code.ShouldBe(ProjectErrors.NotFoundCode);
    }

    [Fact]
    public async Task SetCadence_ByManager_ChangesItSavesAndAudits()
    {
        var repository = TestProjects.RepositoryHolding(_project);

        var result = await Setter(TestUsers.Manager(), repository).HandleAsync(new SetSprintCadence(_project.Id, 4), CancellationToken.None);

        result.Value.Weeks.ShouldBe(4);
        _project.SprintWeeks.ShouldBe(4);
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _audit.Received(1).WriteAsync(
            Arg.Is<AuditRecord>(r => r.Action == AuditActions.SprintCadenceChanged), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetCadence_ToAnUnsupportedLength_IsRefusedAndNothingChanges()
    {
        var result = await Setter(TestUsers.Manager()).HandleAsync(new SetSprintCadence(_project.Id, 5), CancellationToken.None);

        result.Error.ValidationErrors.ShouldNotBeNull().ShouldContainKey("weeks");
        _project.SprintWeeks.ShouldBe(2);
    }

    [Fact]
    public async Task SetCadence_ByAMemberWhoIsNotAManager_IsForbidden()
    {
        var result = await Setter(TestUsers.Architect()).HandleAsync(new SetSprintCadence(_project.Id, 3), CancellationToken.None);

        result.Error.Code.ShouldBe(ProjectErrors.ManagerRequiredCode);
        _project.SprintWeeks.ShouldBe(2);
    }
}
