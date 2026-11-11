namespace AiSdlc.Domain.Artifacts;

/// <summary>The result of trying to approve or reject a version at a gate.</summary>
public enum GateOutcome
{
    Done,

    /// <summary>There is no such version.</summary>
    NotFound,

    /// <summary>Only the newest version can be decided.</summary>
    NotLatest,

    /// <summary>The version has already been decided or replaced.</summary>
    NotPending,

    /// <summary>The project is not at the stage this gate belongs to.</summary>
    WrongStage,
}
