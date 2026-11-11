using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using AiSdlc.Application.Common;
using AiSdlc.Application.Files;
using AiSdlc.Application.Projects;
using AiSdlc.Domain.Auditing;
using AiSdlc.Domain.Projects;
using AiSdlc.Infrastructure.Persistence;
using AiSdlc.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace AiSdlc.IntegrationTests.Projects;

[Collection(SharedApi.Name)]
public sealed class ProjectsAndFilesTests(ApiFactory factory)
{
    private const string Requirements = "requirements";
    private const string TeamTemplate = "team-template";

    [Fact]
    public async Task Manager_CreatesAProject_AndOnlyMembersCanSeeIt()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateProjectAsync();
        using var architect = await factory.SignedInAsClientAsync(TestUsers.Architect);

        (await architect.GetAsync($"/api/v1/projects/{project.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        await manager.AddMemberAsync(project.Id, (await manager.FindUserAsync(TestUsers.Architect)).Id);

        var seen = await architect.GetProjectAsync(project.Id);
        seen.CanManage.ShouldBeFalse();
        seen.Members.Select(m => m.Email).ShouldBe([TestUsers.ProjectManager, TestUsers.Architect], ignoreOrder: true);
        var list = await architect.GetFromJsonAsync<PagedResult<ProjectSummaryDto>>("/api/v1/projects?pageSize=100", ApiJson.Options);
        list!.Items.ShouldContain(p => p.Id == project.Id);
    }

    [Fact]
    public async Task UploadingBothInputs_VersionsThemAndMovesTheProjectToFilesUploaded()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateProjectAsync();

        var first = await manager.UploadOkAsync(project.Id, Requirements, "reqs.docx", TestFiles.Docx("one"));
        var second = await manager.UploadOkAsync(project.Id, Requirements, "reqs.docx", TestFiles.Docx("two"));
        await manager.UploadOkAsync(project.Id, TeamTemplate, "team.xlsx", TestFiles.Xlsx());

        first.Version.ShouldBe(1);
        second.Version.ShouldBe(2);
        var detail = await manager.GetProjectAsync(project.Id);
        detail.Stage.ShouldBe(ProjectStage.FilesUploaded);
        detail.Files.Where(f => f.Kind == FileKind.Requirements).Select(f => (f.Version, f.IsLatest))
            .ShouldBe([(2, true), (1, false)]);
    }

    [Fact]
    public async Task Download_ReturnsTheExactBytesAsAnAttachmentWithAServerChosenType()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateProjectAsync();
        var bytes = TestFiles.Docx("download me");
        var uploaded = await manager.UploadOkAsync(project.Id, Requirements, "My Requirements.docx", bytes);
        await manager.AddMemberAsync(project.Id, (await manager.FindUserAsync(TestUsers.Developer)).Id);
        using var developer = await factory.SignedInAsClientAsync(TestUsers.Developer);

        var response = await developer.GetAsync($"/api/v1/projects/{project.Id}/files/{uploaded.Id}/download");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsByteArrayAsync()).ShouldBe(bytes);
        response.Content.Headers.ContentType!.MediaType.ShouldBe(FileRules.DocxContentType);
        response.Content.Headers.ContentDisposition!.DispositionType.ShouldBe("attachment");
        response.Content.Headers.ContentDisposition.FileName.ShouldBe("My Requirements.docx");
        uploaded.Sha256.ShouldBe(Convert.ToHexString(SHA256.HashData(bytes)));
    }

    [Fact]
    public async Task MemberWhoIsNotAManager_CannotUploadOrDelete_AndANonMemberLearnsNothing()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateProjectAsync();
        var file = await manager.UploadOkAsync(project.Id, Requirements, "r.txt", TestFiles.Text());
        await manager.AddMemberAsync(project.Id, (await manager.FindUserAsync(TestUsers.QA)).Id);
        using var qa = await factory.SignedInAsClientAsync(TestUsers.QA);
        using var outsider = await factory.SignedInAsClientAsync(TestUsers.Developer);

        // QA is a member but not a manager. The policy answers 403 before the project is even looked at.
        (await qa.UploadAsync(project.Id, Requirements, "x.txt", TestFiles.Text())).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await qa.DeleteAsync($"/api/v1/projects/{project.Id}/files/{file.Id}")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await outsider.GetAsync($"/api/v1/projects/{project.Id}/files/{file.Id}/download")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(Requirements, "malware.exe", "text")]
    [InlineData(Requirements, "fake.docx", "text")]
    [InlineData(Requirements, "sheet.xlsx", "xlsx")]
    [InlineData(TeamTemplate, "doc.docx", "docx")]
    [InlineData(Requirements, "empty.txt", "empty")]
    [InlineData(Requirements, "macro.docx", "macro")]
    [InlineData(Requirements, "binary.txt", "binary")]
    public async Task HostileOrMismatchedUploads_AreRejectedWith400AndNothingIsKept(string kind, string name, string content)
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateProjectAsync();
        var bytes = content switch
        {
            "xlsx" => TestFiles.Xlsx(),
            "docx" => TestFiles.Docx(),
            "empty" => [],
            "macro" => TestFiles.Package(string.Empty, "word/document.xml", "word/vbaProject.bin"),
            "binary" => [0x41, 0x00, 0x42],
            _ => TestFiles.Text("plain words"),
        };

        var response = await manager.UploadAsync(project.Id, kind, name, bytes);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = (await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Http.HttpValidationProblemDetails>(ApiJson.Options))!;
        problem.Errors.ShouldContainKey("file");
        (await manager.GetProjectAsync(project.Id)).Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task UnknownFileKind_Returns404()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateProjectAsync();

        var response = await manager.UploadAsync(project.Id, "secrets", "a.txt", TestFiles.Text());

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task OversizedUpload_IsRefused()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateProjectAsync();
        var justOver = new byte[FileRules.MaxFileBytes + 1];
        Array.Fill(justOver, (byte)'a');
        var wayOver = new byte[(12 * 1024 * 1024)];
        Array.Fill(wayOver, (byte)'a');

        (await manager.UploadAsync(project.Id, Requirements, "over.txt", justOver)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await manager.UploadAsync(project.Id, Requirements, "way-over.txt", wayOver)).StatusCode
            .ShouldBeOneOf(HttpStatusCode.RequestEntityTooLarge, HttpStatusCode.BadRequest);
        (await manager.GetProjectAsync(project.Id)).Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task PathTricksInTheFileName_NeverReachTheStoredNameOrTheDisk()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateProjectAsync();

        var file = await manager.UploadOkAsync(project.Id, Requirements, "..\\..\\..\\evil.txt", TestFiles.Text());

        file.FileName.ShouldBe("evil.txt");
    }

    [Fact]
    public async Task Delete_RemovesTheFile_ReopensTheStage_AndKeepsVersionNumbersMoving()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateProjectAsync();
        await manager.UploadOkAsync(project.Id, TeamTemplate, "team.xlsx", TestFiles.Xlsx());
        var requirements = await manager.UploadOkAsync(project.Id, Requirements, "r.txt", TestFiles.Text());
        (await manager.GetProjectAsync(project.Id)).Stage.ShouldBe(ProjectStage.FilesUploaded);

        var deleted = await manager.DeleteAsync($"/api/v1/projects/{project.Id}/files/{requirements.Id}");

        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await manager.GetAsync($"/api/v1/projects/{project.Id}/files/{requirements.Id}/download")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await manager.GetProjectAsync(project.Id)).Stage.ShouldBe(ProjectStage.Draft);
        (await manager.UploadOkAsync(project.Id, Requirements, "r2.txt", TestFiles.Text("again"))).Version.ShouldBe(2);
    }

    [Fact]
    public async Task UploadRejectionAndDeletion_AreWrittenToTheAuditTrail()
    {
        using var manager = await factory.SignedInAsClientAsync(TestUsers.ProjectManager);
        var project = await manager.CreateProjectAsync();
        await manager.UploadAsync(project.Id, Requirements, "bad.docx", TestFiles.Text("not a zip"));
        var file = await manager.UploadOkAsync(project.Id, Requirements, "good.txt", TestFiles.Text());
        await manager.DeleteAsync($"/api/v1/projects/{project.Id}/files/{file.Id}");

        await using var scope = factory.Services.CreateAsyncScope();
        var actions = await scope.ServiceProvider.GetRequiredService<AiSdlcDbContext>().AuditEntries
            .AsNoTracking().Where(e => e.Details!.StartsWith(project.Id.ToString())).Select(e => e.Action).ToListAsync();

        actions.ShouldContain(AuditActions.FileRejected);
        actions.ShouldContain(AuditActions.FileUploaded);
        actions.ShouldContain(AuditActions.FileDeleted);
    }
}
