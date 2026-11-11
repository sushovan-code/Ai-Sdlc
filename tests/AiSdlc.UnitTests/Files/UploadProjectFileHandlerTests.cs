using System.Security.Cryptography;
using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Application.Files;
using AiSdlc.Application.Files.UploadProjectFile;
using AiSdlc.Application.Projects;
using AiSdlc.Domain.Auditing;
using AiSdlc.Domain.Projects;
using AiSdlc.Domain.Security;
using AiSdlc.UnitTests.Support;
using NSubstitute;
using Shouldly;

namespace AiSdlc.UnitTests.Files;

public sealed class UploadProjectFileHandlerTests
{
    private readonly Project _project = TestProjects.WithTeam();
    private readonly IUserAccounts _accounts = Substitute.For<IUserAccounts>();
    private readonly IFileStorage _storage = Substitute.For<IFileStorage>();
    private readonly IAuditLog _audit = Substitute.For<IAuditLog>();
    private readonly IProjectRepository _repository;

    public UploadProjectFileHandlerTests()
    {
        _repository = TestProjects.RepositoryHolding(_project);
        _accounts.FindAsync(TestUsers.ManagerId, Arg.Any<CancellationToken>())
            .Returns(TestUsers.User(TestUsers.ManagerId, AppRoles.ProjectManager, "Pat Manager"));
    }

    private UploadProjectFileHandler HandlerFor(ICurrentUser caller) =>
        new(caller, _repository, _accounts, _storage, _audit, new FixedTime(TestProjects.Now));

    private UploadProjectFile Upload(FileKind kind, string name, byte[] content) =>
        new(_project.Id, kind, name, new MemoryStream(content));

    [Fact]
    public async Task Handle_ValidDocx_StoresItHashedUnderAGeneratedKeyAsVersionOne()
    {
        var bytes = TestFiles.Docx();
        byte[]? stored = null;
        string? storedKey = null;
        await _storage.SaveAsync(
            Arg.Do<string>(k => storedKey = k),
            Arg.Do<Stream>(s => { using var copy = new MemoryStream(); s.CopyTo(copy); stored = copy.ToArray(); }),
            Arg.Any<CancellationToken>());

        var result = await HandlerFor(TestUsers.Manager())
            .HandleAsync(Upload(FileKind.Requirements, "Sample requirements.docx", bytes), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Version.ShouldBe(1);
        result.Value.IsLatest.ShouldBeTrue();
        result.Value.FileName.ShouldBe("Sample requirements.docx");
        result.Value.SizeBytes.ShouldBe(bytes.Length);
        result.Value.Sha256.ShouldBe(Convert.ToHexString(SHA256.HashData(bytes)));
        result.Value.UploadedByName.ShouldBe("Pat Manager");
        stored.ShouldBe(bytes);
        storedKey.ShouldStartWith($"projects/{_project.Id:N}/");
        storedKey.ShouldNotContain("requirements");
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _audit.Received(1).WriteAsync(
            Arg.Is<AuditRecord>(r => r.Action == AuditActions.FileUploaded && r.Outcome == AuditOutcome.Success),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SecondUpload_BecomesVersionTwo()
    {
        var handler = HandlerFor(TestUsers.Manager());
        await handler.HandleAsync(Upload(FileKind.Requirements, "a.txt", TestFiles.Text()), CancellationToken.None);

        var second = await handler.HandleAsync(Upload(FileKind.Requirements, "b.txt", TestFiles.Text("Version two")), CancellationToken.None);

        second.Value.Version.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_DirectoryTricksInTheName_AreStrippedFromTheStoredName()
    {
        var result = await HandlerFor(TestUsers.Manager())
            .HandleAsync(Upload(FileKind.Requirements, "..\\..\\evil.txt", TestFiles.Text()), CancellationToken.None);

        result.Value.FileName.ShouldBe("evil.txt");
    }

    [Fact]
    public async Task Handle_NotAMember_GetsNotFoundAndNothingIsStored()
    {
        var result = await HandlerFor(TestUsers.Outsider())
            .HandleAsync(Upload(FileKind.Requirements, "a.txt", TestFiles.Text()), CancellationToken.None);

        result.Error.Code.ShouldBe(ProjectErrors.NotFoundCode);
        await _storage.DidNotReceiveWithAnyArgs().SaveAsync(default!, default!, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_MemberWhoIsNotAManager_IsForbiddenAndNothingIsStored()
    {
        var result = await HandlerFor(TestUsers.Developer())
            .HandleAsync(Upload(FileKind.Requirements, "a.txt", TestFiles.Text()), CancellationToken.None);

        result.Error.Code.ShouldBe(ProjectErrors.ManagerRequiredCode);
        await _storage.DidNotReceiveWithAnyArgs().SaveAsync(default!, default!, CancellationToken.None);
    }

    [Theory]
    [InlineData(FileKind.Requirements, "malware.exe")]
    [InlineData(FileKind.Requirements, "sheet.xlsx")]
    [InlineData(FileKind.TeamTemplate, "doc.docx")]
    [InlineData(FileKind.Requirements, "noextension")]
    public async Task Handle_WrongExtensionForTheKind_IsRejectedAndTheRejectionIsAudited(FileKind kind, string name)
    {
        var result = await HandlerFor(TestUsers.Manager())
            .HandleAsync(Upload(kind, name, TestFiles.Docx()), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Validation);
        await _storage.DidNotReceiveWithAnyArgs().SaveAsync(default!, default!, CancellationToken.None);
        await _audit.Received(1).WriteAsync(
            Arg.Is<AuditRecord>(r => r.Action == AuditActions.FileRejected && r.Outcome == AuditOutcome.Failure),
            Arg.Any<CancellationToken>());
        _project.Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_ContentThatDoesNotMatchTheExtension_IsRejected()
    {
        var result = await HandlerFor(TestUsers.Manager())
            .HandleAsync(Upload(FileKind.Requirements, "fake.docx", TestFiles.Text("not a zip")), CancellationToken.None);

        result.Error.Code.ShouldBe(FileContentInspector.RejectedCode);
        await _storage.DidNotReceiveWithAnyArgs().SaveAsync(default!, default!, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_FileOverTheLimit_IsRejectedWithoutStoring()
    {
        var oversized = new byte[FileRules.MaxFileBytes + 1];
        Array.Fill(oversized, (byte)'a');

        var result = await HandlerFor(TestUsers.Manager())
            .HandleAsync(Upload(FileKind.Requirements, "big.txt", oversized), CancellationToken.None);

        result.Error.ValidationErrors!["file"][0].ShouldContain("10 MB");
        await _storage.DidNotReceiveWithAnyArgs().SaveAsync(default!, default!, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_FileExactlyAtTheLimit_IsAccepted()
    {
        var exact = new byte[FileRules.MaxFileBytes];
        Array.Fill(exact, (byte)'a');

        var result = await HandlerFor(TestUsers.Manager())
            .HandleAsync(Upload(FileKind.Requirements, "exact.txt", exact), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.SizeBytes.ShouldBe(FileRules.MaxFileBytes);
    }

    [Fact]
    public async Task Handle_WhenSavingTheRowFails_TheStoredContentIsRemoved()
    {
        string? storedKey = null;
        await _storage.SaveAsync(Arg.Do<string>(k => storedKey = k), Arg.Any<Stream>(), Arg.Any<CancellationToken>());
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("db down")));

        await Should.ThrowAsync<InvalidOperationException>(() => HandlerFor(TestUsers.Manager())
            .HandleAsync(Upload(FileKind.Requirements, "a.txt", TestFiles.Text()), CancellationToken.None));

        await _storage.Received(1).DeleteAsync(storedKey!, Arg.Any<CancellationToken>());
    }
}
