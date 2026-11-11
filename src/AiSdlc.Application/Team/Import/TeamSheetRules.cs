namespace AiSdlc.Application.Team.Import;

/// <summary>What a team sheet must look like.</summary>
public static class TeamSheetRules
{
    public const string NameColumn = "Name";
    public const string RoleColumn = "Role";
    public const string SkillsColumn = "Skills";
    public const string CapacityColumn = "Capacity";
    public const string AvailabilityColumn = "Availability";

    public const int MaxPeople = 500;
    public const int MaxNameLength = 100;
    public const int MaxRoleLength = 50;
    public const int MaxSkillLength = 50;
    public const int MaxSkills = 20;

    /// <summary>How many problems are listed before the rest are summarised.</summary>
    public const int MaxProblemsReported = 10;

    public static IReadOnlyList<string> RequiredColumns { get; } =
        [NameColumn, RoleColumn, SkillsColumn, CapacityColumn, AvailabilityColumn];
}
