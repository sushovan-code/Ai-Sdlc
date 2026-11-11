using System.Net;
using System.Net.Http.Json;
using AiSdlc.Application.Stages;
using AiSdlc.Domain.Projects;
using AiSdlc.IntegrationTests.Infrastructure;
using Shouldly;

namespace AiSdlc.IntegrationTests.Projects;

[Collection(SharedApi.Name)]
public sealed class StageFlowTests(ApiFactory factory)
{
    private static string Url(Guid project, string stage, string suffix = "") =>
        $"/api/v1/projects/{project}/stages/{stage}{suffix}";

    private static async Task<StageVersionDto> GenerateAsync(HttpClient client, Guid project, string stage)
    {
        var response = await client.PostAsJsonAsync(Url(project, stage, "/generate"), new { });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<StageVersionDto>(ApiJson.Options))!;
    }

    [Fact]
    public async Task AProjectCanTravelThroughAllFourGates_EachApprovedByItsOwnRole()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateReadyProjectAsync();
        await manager.AddMemberAsync(project, (await manager.FindUserAsync(TestUsers.Architect)).Id);
        await manager.AddMemberAsync(project, (await manager.FindUserAsync(TestUsers.QA)).Id);
        using var architect = await factory.SignedInAsClientAsync(TestUsers.Architect);
        using var qa = await factory.SignedInAsClientAsync(TestUsers.QA);

        // Gate 1 by the Project Manager.
        await manager.GenerateAsync(project);
        (await manager.PostAsJsonAsync($"/api/v1/projects/{project}/analysis/versions/1/approve", new { })).StatusCode
            .ShouldBe(HttpStatusCode.NoContent);

        // Gate 2: only the Architect, and the Project Manager is refused.
        (await manager.PostAsJsonAsync(Url(project, "architecture", "/generate"), new { })).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var architecture = await GenerateAsync(architect, project, "architecture");
        architecture.Content.Sections.SelectMany(s => s.Items).ShouldAllBe(i => i.Id.Length > 0);
        (await architect.PostAsJsonAsync(Url(project, "architecture", "/versions/1/approve"), new { })).StatusCode
            .ShouldBe(HttpStatusCode.NoContent);

        // Gate 3: tasks are placed by the scheduler, and the team page shows booked hours.
        var tasks = await GenerateAsync(manager, project, "tasks");
        tasks.Notes.Assignments.ShouldNotBeEmpty();
        (await manager.GetTeamAsync(project)).TotalAllocatedHours.ShouldBeGreaterThan(0);
        (await manager.PostAsJsonAsync(Url(project, "tasks", "/versions/1/approve"), new { })).StatusCode
            .ShouldBe(HttpStatusCode.NoContent);

        // Gate 4 by QA, the final gate.
        (await architect.PostAsJsonAsync(Url(project, "test-cases", "/generate"), new { })).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var cases = await GenerateAsync(qa, project, "test-cases");
        cases.Content.Sections.SelectMany(s => s.Items).ShouldNotBeEmpty();
        (await qa.PostAsJsonAsync(Url(project, "test-cases", "/versions/1/approve"), new { })).StatusCode
            .ShouldBe(HttpStatusCode.NoContent);

        (await manager.GetProjectAsync(project)).Stage.ShouldBe(ProjectStage.TestCasesApproved);
    }

    [Fact]
    public async Task ALaterStage_CannotBeDraftedBeforeTheEarlierGateIsPassed()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateReadyProjectAsync();
        await manager.AddMemberAsync(project, (await manager.FindUserAsync(TestUsers.Architect)).Id);
        using var architect = await factory.SignedInAsClientAsync(TestUsers.Architect);

        var response = await architect.PostAsJsonAsync(Url(project, "architecture", "/generate"), new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task EditingADraft_SavesANewHumanVersion_AndStageEndpointsRejectAnonymousCalls()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateReadyProjectAsync();
        await manager.AddMemberAsync(project, (await manager.FindUserAsync(TestUsers.Architect)).Id);
        await manager.GenerateAsync(project);
        await manager.PostAsJsonAsync($"/api/v1/projects/{project}/analysis/versions/1/approve", new { });
        using var architect = await factory.SignedInAsClientAsync(TestUsers.Architect);
        var draft = await GenerateAsync(architect, project, "architecture");

        var edited = draft.Content with { Summary = "Edited by the architect." };
        var response = await architect.PutAsJsonAsync(Url(project, "architecture", "/edit"), edited);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var version = (await response.Content.ReadFromJsonAsync<StageVersionDto>(ApiJson.Options))!;
        version.Version.ShouldBe(2);
        version.Source.ToString().ShouldBe("Human");
        using var anonymous = factory.CreateClient();
        (await anonymous.GetAsync(Url(project, "architecture"))).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EditingWithAnIncompleteBody_IsRefusedCleanly_NeverAServerError()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateReadyProjectAsync();
        await manager.AddMemberAsync(project, (await manager.FindUserAsync(TestUsers.Architect)).Id);
        await manager.GenerateAsync(project);
        await manager.PostAsJsonAsync($"/api/v1/projects/{project}/analysis/versions/1/approve", new { });
        using var architect = await factory.SignedInAsClientAsync(TestUsers.Architect);
        await GenerateAsync(architect, project, "architecture");
        using var outsider = await factory.SignedInAsClientAsync(TestUsers.QA);
        const string incomplete = "{\"summary\":\"x\",\"sections\":null}";

        using var asOutsider = new StringContent(incomplete, System.Text.Encoding.UTF8, "application/json");
        using var asArchitect = new StringContent(incomplete, System.Text.Encoding.UTF8, "application/json");

        (await outsider.PutAsync(Url(project, "architecture", "/edit"), asOutsider)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await architect.PutAsync(Url(project, "architecture", "/edit"), asArchitect)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
