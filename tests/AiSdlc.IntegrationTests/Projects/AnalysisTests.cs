using System.Net;
using System.Net.Http.Json;
using AiSdlc.Application.Analysis;
using AiSdlc.Domain.Artifacts;
using AiSdlc.Domain.Projects;
using AiSdlc.IntegrationTests.Infrastructure;
using Shouldly;

namespace AiSdlc.IntegrationTests.Projects;

[Collection(SharedApi.Name)]
public sealed class AnalysisTests(ApiFactory factory)
{
    private const string Requirements = "requirements";
    private const string TeamTemplate = "team-template";

    [Theory]
    [InlineData("GET", "")]
    [InlineData("POST", "/generate")]
    [InlineData("POST", "/versions/1/approve")]
    [InlineData("POST", "/versions/1/reject")]
    public async Task EveryEndpoint_RejectsAnonymousCalls(string method, string suffix)
    {
        using var anonymous = factory.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod(method), $"/api/v1/projects/{Guid.NewGuid()}/analysis{suffix}");

        (await anonymous.SendAsync(request)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/generate")]
    [InlineData("/versions/1/approve")]
    [InlineData("/versions/1/reject")]
    public async Task ChangingEndpoints_AreForbiddenForAMemberWhoIsNotAProjectManager(string suffix)
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateReadyProjectAsync();
        await manager.AddMemberAsync(project, (await manager.FindUserAsync(TestUsers.Developer)).Id);
        using var developer = await factory.SignedInAsClientAsync(TestUsers.Developer);

        var response = await developer.PostAsJsonAsync($"/api/v1/projects/{project}/analysis{suffix}", new { comment = "x" });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ANonMember_CannotReadTheAnalysis()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateReadyProjectAsync();
        using var outsider = await factory.SignedInAsClientAsync(TestUsers.QA);

        (await outsider.GetAsync($"/api/v1/projects/{project}/analysis")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Generating_BeforeBothFilesAreUploaded_IsRefused()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var created = await manager.CreateProjectAsync();

        var response = await manager.PostAsJsonAsync($"/api/v1/projects/{created.Id}/analysis/generate", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var overview = await manager.GetAnalysisAsync(created.Id);
        overview.CanGenerate.ShouldBeFalse();
        overview.BlockedReason.ShouldNotBeNull();
    }

    [Fact]
    public async Task Generating_StoresAPendingVersion_WithEvidenceFromTheDocument()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateReadyProjectAsync();

        var version = await manager.GenerateAsync(project);

        version.Version.ShouldBe(1);
        version.Status.ShouldBe(ArtifactStatus.PendingReview);
        version.Source.ShouldBe(ArtifactSource.Ai);
        version.Content.FunctionalRequirements.ShouldNotBeEmpty();
        version.Content.FunctionalRequirements.ShouldAllBe(r => r.SourceRefs.Count > 0);
        version.Content.Evidence.ShouldNotBeEmpty();
        version.Content.NonFunctionalRequirements.ShouldNotBeEmpty();
        (await manager.GetAnalysisAsync(project)).Versions.ShouldHaveSingleItem().Version.ShouldBe(1);
    }

    [Fact]
    public async Task Regenerating_WithFeedback_AddsANewVersion_AndSupersedesThePreviousOne()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateReadyProjectAsync();
        await manager.GenerateAsync(project);

        var second = await manager.GenerateAsync(project, "Focus on refunds");

        second.Version.ShouldBe(2);
        second.BasedOnVersion.ShouldBe(1);
        second.Feedback.ShouldBe("Focus on refunds");
        second.Content.Assumptions.ShouldContain(a => a.Contains("Focus on refunds", StringComparison.Ordinal));
        var versions = (await manager.GetAnalysisAsync(project)).Versions;
        versions.Select(v => (v.Version, v.Status)).ShouldBe([(2, ArtifactStatus.PendingReview), (1, ArtifactStatus.Superseded)]);
    }

    [Fact]
    public async Task Approving_RecordsTheReviewer_MovesTheProjectOn_AndLocksTheRequirements()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateReadyProjectAsync();
        await manager.GenerateAsync(project);

        var approve = await manager.PostAsJsonAsync($"/api/v1/projects/{project}/analysis/versions/1/approve", new { comment = "Looks right" });

        approve.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var detail = await manager.GetProjectAsync(project);
        detail.Stage.ShouldBe(ProjectStage.AnalysisApproved);
        var decision = (await manager.GetAnalysisAsync(project)).Versions.Single().Decisions.ShouldHaveSingleItem();
        decision.Kind.ShouldBe(ReviewDecisionKind.Approved);
        decision.ReviewerRole.ShouldBe("ProjectManager");
        decision.Comment.ShouldBe("Looks right");

        (await manager.PostAsJsonAsync($"/api/v1/projects/{project}/analysis/generate", new { })).StatusCode
            .ShouldBe(HttpStatusCode.Conflict);
        (await manager.UploadAsync(project, Requirements, "changed.txt", TestFiles.Text("New text"))).StatusCode
            .ShouldBe(HttpStatusCode.Conflict);
        var requirementsFile = detail.Files.Single(f => f.Kind == FileKind.Requirements);
        (await manager.DeleteAsync($"/api/v1/projects/{project}/files/{requirementsFile.Id}")).StatusCode
            .ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task OnlyTheNewestVersion_CanBeDecided()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateReadyProjectAsync();
        await manager.GenerateAsync(project);
        await manager.GenerateAsync(project, "again");

        var old = await manager.PostAsJsonAsync($"/api/v1/projects/{project}/analysis/versions/1/approve", new { });
        var missing = await manager.PostAsJsonAsync($"/api/v1/projects/{project}/analysis/versions/9/approve", new { });

        old.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        missing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Rejecting_NeedsAComment_AndKeepsTheProjectAtTheGate()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateReadyProjectAsync();
        await manager.GenerateAsync(project);

        var withoutComment = await manager.PostAsJsonAsync($"/api/v1/projects/{project}/analysis/versions/1/reject", new { });
        var rejected = await manager.PostAsJsonAsync($"/api/v1/projects/{project}/analysis/versions/1/reject", new { comment = "Too thin" });
        var again = await manager.PostAsJsonAsync($"/api/v1/projects/{project}/analysis/versions/1/approve", new { });

        withoutComment.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        rejected.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        again.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await manager.GetProjectAsync(project)).Stage.ShouldBe(ProjectStage.FilesUploaded);
        (await manager.GetAnalysisAsync(project)).CanGenerate.ShouldBeTrue();
        (await manager.GenerateAsync(project, "Too thin, add detail")).Version.ShouldBe(2);
    }

    [Fact]
    public async Task AWordDocumentThatCannotBeRead_IsRefusedWithAClearMessage()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var created = await manager.CreateProjectAsync();
        await manager.UploadOkAsync(created.Id, Requirements, "broken.docx", TestFiles.Docx());
        await manager.UploadOkAsync(created.Id, TeamTemplate, "team.xlsx", TestFiles.Xlsx());

        var response = await manager.PostAsJsonAsync($"/api/v1/projects/{created.Id}/analysis/generate", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
