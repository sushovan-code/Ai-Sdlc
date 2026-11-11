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

    /// <summary>Gate 2 is passed: the architecture plan was approved, so tasks can be generated.</summary>
    ArchitectureApproved,

    /// <summary>Gate 3 is passed: tasks and assignments were approved, so test cases can be generated.</summary>
    TasksApproved,

    /// <summary>Gate 4 is passed: the test cases were approved. The lifecycle is complete.</summary>
    TestCasesApproved,
}
