namespace AiSdlc.Domain.Projects;

/// <summary>Where a project is in the lifecycle. Later slices add the review stages after <see cref="FilesUploaded"/>.</summary>
public enum ProjectStage
{
    /// <summary>The project exists but both input files are not uploaded yet.</summary>
    Draft,

    /// <summary>Requirements and the team sheet are both present.</summary>
    FilesUploaded,

    /// <summary>Gate 1 is passed: the requirements analysis was approved, so architecture can begin.</summary>
    AnalysisApproved,
}
