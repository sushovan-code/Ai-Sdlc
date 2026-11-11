using System.Net;
using System.Net.Http.Json;
using AiSdlc.Application.Common;
using AiSdlc.Application.Stages;
using AiSdlc.Application.Wrapup;
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

        // Wrap-up: traceability reaches test cases, the audit trail names the decisions, and the report downloads.
        var trace = await manager.GetFromJsonAsync<TraceabilityDto>($"/api/v1/projects/{project}/traceability", ApiJson.Options);
        trace!.Rows.ShouldContain(r => r.Architecture.Count > 0 && r.Tasks.Count > 0 && r.TestCases.Count > 0);
        var audit = await manager.GetFromJsonAsync<PagedResult<AuditEntryDto>>($"/api/v1/projects/{project}/audit", ApiJson.Options);
        audit!.Items.Select(e => e.Action).ShouldContain("stage.approved");
        audit.Items.Select(e => e.Action).ShouldContain("ai.call");
        var report = await manager.GetAsync($"/api/v1/projects/{project}/report");
        report.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await report.Content.ReadAsStringAsync()).ShouldContain("## QA test cases");
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
}
