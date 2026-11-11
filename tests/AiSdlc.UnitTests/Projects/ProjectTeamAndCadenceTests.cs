using AiSdlc.Domain.Projects;
using AiSdlc.UnitTests.Support;
using Shouldly;

namespace AiSdlc.UnitTests.Projects;

public sealed class ProjectTeamAndCadenceTests
{
    private static readonly DateTimeOffset Now = TestProjects.Now;

    private static TeamMemberDraft Person(string name, decimal availability = 30) =>
        new(name, "Developer", ["C#"], 40, availability);

    private static ProjectFile AddTeamFile(Project project, params TeamMemberDraft[] people)
    {
        var file = project.AddFile(FileKind.TeamTemplate, "team.xlsx", 10, new string('C', 64), $"k/{Guid.NewGuid():N}", TestUsers.ManagerId, Now);
        project.AttachTeam(file, people);
        return file;
    }

    [Fact]
    public void NewProject_UsesTwoWeekSprints()
    {
        TestProjects.WithTeam().SprintWeeks.ShouldBe(2);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void SetSprintCadence_AcceptsTwoThreeOrFour(int weeks)
    {
        var project = TestProjects.WithTeam();

        project.SetSprintCadence(weeks).ShouldBeTrue();

        project.SprintWeeks.ShouldBe(weeks);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(-3)]
    public void SetSprintCadence_RefusesOtherLengths_AndKeepsTheCurrentOne(int weeks)
    {
        var project = TestProjects.WithTeam();
        project.SetSprintCadence(3);

        project.SetSprintCadence(weeks).ShouldBeFalse();

        project.SprintWeeks.ShouldBe(3);
    }

    [Fact]
    public void CurrentTeam_IsEmptyBeforeATeamSheetIsUploaded()
    {
        TestProjects.WithTeam().CurrentTeam.ShouldBeEmpty();
    }

    [Fact]
    public void CurrentTeam_IsThePeopleOnTheLatestSheet()
    {
        var project = TestProjects.WithTeam();
        AddTeamFile(project, Person("Asha"), Person("Ravi"));

        project.CurrentTeam.Select(m => m.Name).ShouldBe(["Asha", "Ravi"], ignoreOrder: true);
    }

    [Fact]
    public void CurrentTeam_AfterANewSheet_ReplacesThePreviousPeople()
    {
        var project = TestProjects.WithTeam();
        AddTeamFile(project, Person("Asha"));
        AddTeamFile(project, Person("Priya"), Person("Meera"));

        project.CurrentTeam.Select(m => m.Name).ShouldBe(["Priya", "Meera"], ignoreOrder: true);
        project.Team.Count.ShouldBe(3);
    }

    [Fact]
    public void CurrentTeam_AfterDeletingTheLatestSheet_FallsBackToThePreviousOne()
    {
        var project = TestProjects.WithTeam();
        AddTeamFile(project, Person("Asha"));
        var latest = AddTeamFile(project, Person("Priya"));

        project.DeleteFile(latest.Id, TestUsers.ManagerId, Now);

        project.CurrentTeam.Select(m => m.Name).ShouldBe(["Asha"]);
    }

    [Fact]
    public void CurrentTeam_AfterDeletingTheOnlySheet_IsEmpty()
    {
        var project = TestProjects.WithTeam();
        var only = AddTeamFile(project, Person("Asha"));

        project.DeleteFile(only.Id, TestUsers.ManagerId, Now);

        project.CurrentTeam.ShouldBeEmpty();
    }

    [Fact]
    public void AttachTeam_ToARequirementsFile_Throws()
    {
        var project = TestProjects.WithTeam();
        var requirements = TestProjects.AddRequirements(project);

        Should.Throw<ArgumentException>(() => project.AttachTeam(requirements, [Person("Asha")]));
    }

    [Fact]
    public void AttachTeam_KeepsHoursAndSkillsFromTheSheet()
    {
        var project = TestProjects.WithTeam();
        AddTeamFile(project, new TeamMemberDraft("Priya", "Developer", ["C#", "Azure"], 32, 28));

        var priya = project.CurrentTeam.Single();

        priya.Skills.ShouldBe(["C#", "Azure"]);
        priya.CapacityHours.ShouldBe(32);
        priya.AvailabilityHours.ShouldBe(28);
    }
}
