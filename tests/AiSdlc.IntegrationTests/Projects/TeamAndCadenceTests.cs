using System.Net;
using System.Net.Http.Json;
using AiSdlc.Application.Team.SetSprintCadence;
using AiSdlc.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace AiSdlc.IntegrationTests.Projects;

[Collection(SharedApi.Name)]
public sealed class TeamAndCadenceTests(ApiFactory factory)
{
    private const string TeamTemplate = "team-template";

    [Fact]
    public async Task UploadingTheSampleTeamSheet_ListsEveryoneWithTheirCapacityAndNothingBookedYet()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateProjectAsync();

        await manager.UploadOkAsync(project.Id, TeamTemplate, "team.xlsx", TestFiles.Xlsx());
        var team = await manager.GetTeamAsync(project.Id);

        team.Members.Select(m => m.Name).ShouldBe(["Arjun Das", "Asha Rao", "Meera Iyer", "Priya Nair", "Ravi Kumar"]);
        var asha = team.Members.Single(m => m.Name == "Asha Rao");
        asha.Role.ShouldBe("Developer");
        asha.Skills.ShouldBe(["C#", "ASP.NET Core", "SQL", "EF Core"]);
        asha.CapacityHours.ShouldBe(40);
        asha.AvailabilityHours.ShouldBe(30);
        asha.SprintCapacityHours.ShouldBe(30);
        asha.AllocatedHours.ShouldBe(0);
        team.SprintWeeks.ShouldBe(2);
        team.HoursPerDay.ShouldBe(8);
        team.TotalCapacityHours.ShouldBe(30 + 40 + 32 + 40 + 30);
    }

    [Fact]
    public async Task ChangingTheSprintLength_ScalesEveryonesSprintCapacity()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateProjectAsync();
        await manager.UploadOkAsync(project.Id, TeamTemplate, "team.xlsx", TestFiles.Xlsx());

        var response = await manager.PutAsJsonAsync($"/api/v1/projects/{project.Id}/sprint-cadence", new { weeks = 4 });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<SprintCadenceDto>())!.Weeks.ShouldBe(4);
        var team = await manager.GetTeamAsync(project.Id);
        team.SprintWeeks.ShouldBe(4);
        team.Members.Single(m => m.Name == "Asha Rao").SprintCapacityHours.ShouldBe(60);
        (await manager.GetProjectAsync(project.Id)).SprintWeeks.ShouldBe(4);
    }

    [Fact]
    public async Task ASprintOfFiveWeeks_IsRefused_AndTheLengthStays()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateProjectAsync();

        var response = await manager.PutAsJsonAsync($"/api/v1/projects/{project.Id}/sprint-cadence", new { weeks = 5 });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await manager.GetProjectAsync(project.Id)).SprintWeeks.ShouldBe(2);
    }

    [Fact]
    public async Task AMemberWhoIsNotAManager_CanSeeTheTeamButNotChangeTheSprintLength()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateProjectAsync();
        await manager.UploadOkAsync(project.Id, TeamTemplate, "team.xlsx", TestFiles.Xlsx());
        await manager.AddMemberAsync(project.Id, (await manager.FindUserAsync(TestUsers.Developer)).Id);
        using var developer = await factory.SignedInAsClientAsync(TestUsers.Developer);

        (await developer.GetTeamAsync(project.Id)).Members.Count.ShouldBe(5);
        (await developer.PutAsJsonAsync($"/api/v1/projects/{project.Id}/sprint-cadence", new { weeks = 3 })).StatusCode
            .ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ANonMember_CannotSeeTheTeam()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateProjectAsync();
        using var outsider = await factory.SignedInAsClientAsync(TestUsers.QA);

        (await outsider.GetAsync($"/api/v1/projects/{project.Id}/team")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ATeamSheetWithBadRows_IsRefusedWithTheRowsNamed_AndNothingIsKept()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateProjectAsync();
        var sheet = TestFiles.Xlsx(
            ["Asha Rao", "Developer", "C#", "30", "35"],
            ["Ravi Kumar", "Developer", "Angular", "90", "40"],
            ["Asha Rao", "QA", "Testing", "40", "40"]);

        var response = await manager.UploadAsync(project.Id, TeamTemplate, "team.xlsx", sheet);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problems = (await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!.Errors["file"];
        problems.ShouldContain("Row 2: Availability (35) cannot be more than Capacity (30).");
        problems.ShouldContain(p => p.StartsWith("Row 3: Capacity (90) is more than 80 hours", StringComparison.Ordinal));
        problems.ShouldContain("Row 4: Asha Rao appears more than once.");
        (await manager.GetProjectAsync(project.Id)).Files.ShouldBeEmpty();
        (await manager.GetTeamAsync(project.Id)).Members.ShouldBeEmpty();
    }

    [Fact]
    public async Task ATeamSheetMissingAColumn_IsRefusedWithAClearMessage()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateProjectAsync();
        var sheet = TestFiles.TeamSheet([["Asha Rao", "Developer", "40", "30"]], ["Name", "Role", "Capacity", "Availability"]);

        var response = await manager.UploadAsync(project.Id, TeamTemplate, "team.xlsx", sheet);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problems = (await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!.Errors["file"];
        problems.ShouldHaveSingleItem().ShouldContain("missing the column(s): Skills");
    }

    [Fact]
    public async Task ANewTeamSheet_ReplacesThePeople_AndDeletingItBringsThePreviousOneBack()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateProjectAsync();
        await manager.UploadOkAsync(project.Id, TeamTemplate, "v1.xlsx", TestFiles.Xlsx());
        var second = await manager.UploadOkAsync(project.Id, TeamTemplate, "v2.xlsx", TestFiles.Xlsx(["Zoe Park", "QA", "Testing", "40", "20"]));

        (await manager.GetTeamAsync(project.Id)).Members.Select(m => m.Name).ShouldBe(["Zoe Park"]);

        (await manager.DeleteAsync($"/api/v1/projects/{project.Id}/files/{second.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await manager.GetTeamAsync(project.Id)).Members.Count.ShouldBe(5);
    }
}
