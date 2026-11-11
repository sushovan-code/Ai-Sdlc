using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Files;
using AiSdlc.Application.Files.DeleteProjectFile;
using AiSdlc.Application.Files.DownloadProjectFile;
using AiSdlc.Application.Projects;
using AiSdlc.Domain.Auditing;
using AiSdlc.Domain.Projects;
using AiSdlc.UnitTests.Support;
using NSubstitute;
using Shouldly;

namespace AiSdlc.UnitTests.Files;

public sealed class DownloadAndDeleteHandlerTests
{
    private readonly Project _project = TestProjects.WithTeam();
    private readonly IFileStorage _storage = Substitute.For<IFileStorage>();
    private readonly IAuditLog _audit = Substitute.For<IAuditLog>();
    private readonly ProjectFile _file;

    public DownloadAndDeleteHandlerTests()
    {
        _file = TestProjects.AddRequirements(_project);
        _storage.OpenReadAsync(_file.StorageKey, Arg.Any<CancellationToken>()).Returns(new MemoryStream([1, 2, 3]));
    }

    private DownloadProjectFileHandler Downloader(ICurrentUser caller) =>
        new(caller, TestProjects.RepositoryHolding(_project), _storage, _audit);

    private DeleteProjectFileHandler Deleter(ICurrentUser caller, IProjectRepository? repository = null) =>
        new(caller, repository ?? TestProjects.RepositoryHolding(_project), _storage, _audit, new FixedTime(TestProjects.Now));

    [Fact]
    public async Task Download_ByAnyMember_ReturnsContentWithAServerChosenContentType()
    {
        var result = await Downloader(TestUsers.Developer())
            .HandleAsync(new DownloadProjectFile(_project.Id, _file.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.FileName.ShouldBe("reqs.docx");
        result.Value.ContentType.ShouldBe(FileRules.DocxContentType);
        await _audit.Received(1).WriteAsync(
            Arg.Is<AuditRecord>(r => r.Action == AuditActions.FileDownloaded), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Download_ByANonMember_GetsNotFound()
    {
        var result = await Downloader(TestUsers.Outsider())
            .HandleAsync(new DownloadProjectFile(_project.Id, _file.Id), CancellationToken.None);

        result.Error.Code.ShouldBe(ProjectErrors.NotFoundCode);
        await _storage.DidNotReceiveWithAnyArgs().OpenReadAsync(default!, CancellationToken.None);
    }

    [Fact]
    public async Task Download_OfADeletedFile_GetsNotFound()
    {
        _project.DeleteFile(_file.Id, TestUsers.ManagerId, TestProjects.Now);

        var result = await Downloader(TestUsers.Manager())
            .HandleAsync(new DownloadProjectFile(_project.Id, _file.Id), CancellationToken.None);

        result.Error.Code.ShouldBe(ProjectErrors.FileNotFoundCode);
    }

    [Fact]
    public async Task Download_WhenTheContentIsMissingFromStorage_GetsNotFound()
    {
        _storage.OpenReadAsync(_file.StorageKey, Arg.Any<CancellationToken>()).Returns((Stream?)null);

        var result = await Downloader(TestUsers.Manager())
            .HandleAsync(new DownloadProjectFile(_project.Id, _file.Id), CancellationToken.None);

        result.Error.Code.ShouldBe(ProjectErrors.FileNotFoundCode);
    }

    [Fact]
    public async Task Delete_ByManager_RemovesContentKeepsTheRowAndAudits()
    {
        var repository = TestProjects.RepositoryHolding(_project);

        var result = await Deleter(TestUsers.Manager(), repository)
            .HandleAsync(new DeleteProjectFile(_project.Id, _file.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _file.IsDeleted.ShouldBeTrue();
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _storage.Received(1).DeleteAsync(_file.StorageKey, Arg.Any<CancellationToken>());
        await _audit.Received(1).WriteAsync(
            Arg.Is<AuditRecord>(r => r.Action == AuditActions.FileDeleted), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_ByAMemberWhoIsNotAManager_IsForbiddenAndNothingChanges()
    {
        var result = await Deleter(TestUsers.Developer())
            .HandleAsync(new DeleteProjectFile(_project.Id, _file.Id), CancellationToken.None);

        result.Error.Code.ShouldBe(ProjectErrors.ManagerRequiredCode);
        _file.IsDeleted.ShouldBeFalse();
        await _storage.DidNotReceiveWithAnyArgs().DeleteAsync(default!, CancellationToken.None);
    }

    [Fact]
    public async Task Delete_UnknownFile_GetsNotFound()
    {
        var result = await Deleter(TestUsers.Manager())
            .HandleAsync(new DeleteProjectFile(_project.Id, Guid.NewGuid()), CancellationToken.None);

        result.Error.Code.ShouldBe(ProjectErrors.FileNotFoundCode);
    }

    [Fact]
    public async Task Delete_TheSameFileTwice_SecondTimeGetsNotFound()
    {
        var handler = Deleter(TestUsers.Manager());
        await handler.HandleAsync(new DeleteProjectFile(_project.Id, _file.Id), CancellationToken.None);

        var again = await handler.HandleAsync(new DeleteProjectFile(_project.Id, _file.Id), CancellationToken.None);

        again.Error.Code.ShouldBe(ProjectErrors.FileNotFoundCode);
    }
}
