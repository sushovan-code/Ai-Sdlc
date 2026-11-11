namespace AiSdlc.Domain.Artifacts;

public enum ArtifactStatus
{
    /// <summary>Waiting for a reviewer.</summary>
    PendingReview,

    Approved,

    Rejected,

    /// <summary>Replaced by a newer version before it was decided.</summary>
    Superseded,
}
