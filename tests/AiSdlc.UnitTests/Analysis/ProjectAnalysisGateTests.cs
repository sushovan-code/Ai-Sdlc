using AiSdlc.Domain.Artifacts;
using AiSdlc.Domain.Projects;
using AiSdlc.UnitTests.Support;
using Shouldly;

namespace AiSdlc.UnitTests.Analysis;

public sealed class ProjectAnalysisGateTests
{
    private static readonly DateTimeOffset Now = TestProjects.Now;
    private const string Role = "ProjectManager";

    private static Project ReadyProject()
    {
        var project = TestProjects.WithTeam();
        TestProjects.AddRequirements(project);
        project.AddFile(FileKind.TeamTemplate, "team.xlsx", 1, new string('B', 64), $"k/{Guid.NewGuid():N}", TestUsers.ManagerId, Now);
        return project;
    }

    private static ArtifactVersion Draft(Project project, string? feedback = null) =>
        project.AddAnalysisDraft("{}", null, ArtifactSource.Ai, "model", "analysis-v1", feedback, TestUsers.ManagerId, Now);

    [Fact]
    public void AddAnalysisDraft_BeforeBothFilesAreUploaded_Throws()
    {
        var project = TestProjects.WithTeam();
        TestProjects.AddRequirements(project);

        Should.Throw<InvalidOperationException>(() => Draft(project));
    }

    [Fact]
    public void AddAnalysisDraft_Again_SupersedesTheWaitingDraftAndKeepsCounting()
    {
        var project = ReadyProject();
        var first = Draft(project);

        var second = Draft(project, "more detail");

        first.Status.ShouldBe(ArtifactStatus.Superseded);
        second.Version.ShouldBe(2);
        second.BasedOnVersion.ShouldBe(1);
        second.Feedback.ShouldBe("more detail");
        second.Status.ShouldBe(ArtifactStatus.PendingReview);
        project.LatestArtifact(ArtifactStage.Analysis).ShouldBe(second);
    }

    [Fact]
    public void ApproveAnalysis_NewestPendingDraft_MovesTheProjectOnAndRecordsTheReviewer()
    {
        var project = ReadyProject();
        var draft = Draft(project);

        var outcome = project.ApproveAnalysis(1, TestUsers.ManagerId, Role, "Good", Now);

        outcome.ShouldBe(GateOutcome.Done);
        project.Stage.ShouldBe(ProjectStage.AnalysisApproved);
        draft.Status.ShouldBe(ArtifactStatus.Approved);
        var decision = draft.Decisions.ShouldHaveSingleItem();
        (decision.Kind, decision.ReviewerId, decision.ReviewerRole, decision.Comment, decision.DecidedAtUtc)
            .ShouldBe((ReviewDecisionKind.Approved, TestUsers.ManagerId, Role, "Good", Now));
    }

    [Fact]
    public void ApproveAnalysis_ThenChangingTheRequirements_IsRefusedBecauseTheyAreLocked()
    {
        var project = ReadyProject();
        Draft(project);
        project.ApproveAnalysis(1, TestUsers.ManagerId, Role, null, Now);

        project.RequirementsLocked.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => TestProjects.AddRequirements(project));
        Should.Throw<InvalidOperationException>(
            () => project.DeleteFile(project.LatestFile(FileKind.Requirements)!.Id, TestUsers.ManagerId, Now));
    }

    [Fact]
    public void ApproveAnalysis_AfterApproval_IsWrongStageAndDraftingStops()
    {
        var project = ReadyProject();
        Draft(project);
        project.ApproveAnalysis(1, TestUsers.ManagerId, Role, null, Now);

        project.ApproveAnalysis(1, TestUsers.ManagerId, Role, null, Now).ShouldBe(GateOutcome.WrongStage);
        project.CanDraftAnalysis.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => Draft(project));
    }

    [Fact]
    public void ApproveAnalysis_OlderVersion_IsNotLatest()
    {
        var project = ReadyProject();
        Draft(project);
        Draft(project);

        project.ApproveAnalysis(1, TestUsers.ManagerId, Role, null, Now).ShouldBe(GateOutcome.NotLatest);
        project.Stage.ShouldBe(ProjectStage.FilesUploaded);
    }

    [Fact]
    public void ApproveAnalysis_UnknownVersion_IsNotFound()
    {
        var project = ReadyProject();
        Draft(project);

        project.ApproveAnalysis(7, TestUsers.ManagerId, Role, null, Now).ShouldBe(GateOutcome.NotFound);
    }

    [Fact]
    public void RejectAnalysis_KeepsTheProjectAtTheGate_AndAllowsAnotherDraft()
    {
        var project = ReadyProject();
        var first = Draft(project);

        project.RejectAnalysis(1, TestUsers.ManagerId, Role, "Too thin", Now).ShouldBe(GateOutcome.Done);

        first.Status.ShouldBe(ArtifactStatus.Rejected);
        project.Stage.ShouldBe(ProjectStage.FilesUploaded);
        Draft(project).Version.ShouldBe(2);
    }

    [Fact]
    public void ApproveAnalysis_AlreadyRejectedVersion_IsNotPending()
    {
        var project = ReadyProject();
        Draft(project);
        project.RejectAnalysis(1, TestUsers.ManagerId, Role, "Too thin", Now);

        project.ApproveAnalysis(1, TestUsers.ManagerId, Role, null, Now).ShouldBe(GateOutcome.NotPending);
    }
}
