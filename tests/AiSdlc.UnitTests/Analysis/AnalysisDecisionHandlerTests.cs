using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Analysis;
using AiSdlc.Application.Analysis.ApproveAnalysis;
using AiSdlc.Application.Analysis.RejectAnalysis;
using AiSdlc.Application.Common;
using AiSdlc.Application.Projects;
using AiSdlc.Domain.Artifacts;
using AiSdlc.Domain.Auditing;
using AiSdlc.Domain.Projects;
using AiSdlc.UnitTests.Support;
using NSubstitute;
using Shouldly;

namespace AiSdlc.UnitTests.Analysis;

public sealed class AnalysisDecisionHandlerTests
{
    private readonly IAuditLog _audit = Substitute.For<IAuditLog>();
    private readonly Project _project = TestProjects.WithTeam();
    private readonly IProjectRepository _projects;

    public AnalysisDecisionHandlerTests()
    {
        TestProjects.AddRequirements(_project);
        _project.AddFile(FileKind.TeamTemplate, "team.xlsx", 1, new string('B', 64), "k/team", TestUsers.ManagerId, TestProjects.Now);
        _project.AddAnalysisDraft("{}", null, ArtifactSource.Ai, "model", "analysis-v1", null, TestUsers.ManagerId, TestProjects.Now);
        _projects = TestProjects.RepositoryHolding(_project);
    }

    private ApproveAnalysisHandler Approver(ICurrentUser caller) =>
        new(new ApproveAnalysisValidator(), caller, _projects, _audit, new FixedTime(TestProjects.Now));

    private RejectAnalysisHandler Rejecter(ICurrentUser caller) =>
        new(new RejectAnalysisValidator(), caller, _projects, _audit, new FixedTime(TestProjects.Now));

    [Fact]
    public async Task Approve_ProjectManager_ApprovesSavesAndAudits()
    {
        var result = await Approver(TestUsers.Manager())
            .HandleAsync(new ApproveAnalysis(_project.Id, 1, "  ok  "), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _project.Stage.ShouldBe(ProjectStage.AnalysisApproved);
        _project.Artifacts.Single().Decisions.Single().Comment.ShouldBe("ok");
        await _projects.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _audit.Received(1).WriteAsync(
            Arg.Is<AuditRecord>(r => r.Action == AuditActions.AnalysisApproved && r.ActorId == TestUsers.ManagerId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_AMemberWhoIsNotAProjectManager_IsForbiddenAndNothingChanges()
    {
        var result = await Approver(TestUsers.Architect()).HandleAsync(new ApproveAnalysis(_project.Id, 1, null), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        _project.Stage.ShouldBe(ProjectStage.FilesUploaded);
        await _projects.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_ANonMember_GetsNotFound()
    {
        var result = await Approver(TestUsers.Outsider()).HandleAsync(new ApproveAnalysis(_project.Id, 1, null), CancellationToken.None);

        result.Error.Code.ShouldBe(ProjectErrors.NotFoundCode);
    }

    [Fact]
    public async Task Approve_AVersionThatDoesNotExist_ReportsItAndAuditsNothing()
    {
        var result = await Approver(TestUsers.Manager()).HandleAsync(new ApproveAnalysis(_project.Id, 5, null), CancellationToken.None);

        result.Error.Code.ShouldBe(AnalysisErrors.VersionNotFoundCode);
        await _audit.DidNotReceive().WriteAsync(Arg.Any<AuditRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_ACommentThatIsTooLong_IsRefused()
    {
        var comment = new string('x', AiLimits.MaxFeedbackLength + 1);

        var result = await Approver(TestUsers.Manager()).HandleAsync(new ApproveAnalysis(_project.Id, 1, comment), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public async Task Reject_WithAReason_RejectsSavesAndAudits()
    {
        var result = await Rejecter(TestUsers.Manager()).HandleAsync(new RejectAnalysis(_project.Id, 1, "Too thin"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _project.Artifacts.Single().Status.ShouldBe(ArtifactStatus.Rejected);
        await _audit.Received(1).WriteAsync(
            Arg.Is<AuditRecord>(r => r.Action == AuditActions.AnalysisRejected),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Reject_WithoutAReason_IsRefused(string reason)
    {
        var result = await Rejecter(TestUsers.Manager()).HandleAsync(new RejectAnalysis(_project.Id, 1, reason), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Validation);
        _project.Artifacts.Single().Status.ShouldBe(ArtifactStatus.PendingReview);
    }

    [Fact]
    public async Task Reject_ADecidedVersion_IsAConflict()
    {
        await Approver(TestUsers.Manager()).HandleAsync(new ApproveAnalysis(_project.Id, 1, null), CancellationToken.None);

        var result = await Rejecter(TestUsers.Manager()).HandleAsync(new RejectAnalysis(_project.Id, 1, "Changed my mind"), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Conflict);
    }
}
