namespace AiSdlc.Domain.Auditing;

/// <summary>Stable action names written to the audit trail.</summary>
public static class AuditActions
{
    public const string UserRegistered = "user.registered";
    public const string LoginSucceeded = "auth.login-succeeded";
    public const string LoginFailed = "auth.login-failed";
    public const string UserLockedOut = "auth.user-locked-out";
    public const string PasswordChanged = "auth.password-changed";
    public const string UserRoleChanged = "user.role-changed";
    public const string UserDeactivated = "user.deactivated";
    public const string UserActivated = "user.activated";
    public const string ProjectCreated = "project.created";
    public const string ProjectMemberAdded = "project.member-added";
    public const string ProjectMemberRemoved = "project.member-removed";
    public const string FileUploaded = "file.uploaded";
    public const string FileRejected = "file.rejected";
    public const string FileDownloaded = "file.downloaded";
    public const string FileDeleted = "file.deleted";
    public const string AiCall = "ai.call";
    public const string AnalysisGenerated = "analysis.generated";
    public const string AnalysisApproved = "analysis.approved";
    public const string AnalysisRejected = "analysis.rejected";
    public const string StageGenerated = "stage.generated";
    public const string StageApproved = "stage.approved";
    public const string StageRejected = "stage.rejected";
    public const string StageEdited = "stage.edited";
    public const string ProjectDeleted = "project.deleted";
    public const string ReportDownloaded = "report.downloaded";
    public const string TeamImported = "team.imported";
    public const string SprintCadenceChanged = "project.sprint-cadence-changed";
}
