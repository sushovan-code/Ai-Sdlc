namespace AiSdlc.Domain.Planning;

/// <summary>The working-time rules behind capacity: an 8-hour day, a 5-day week, and 2, 3 or 4-week sprints.</summary>
public static class SprintRules
{
    public const int WorkingHoursPerDay = 8;
    public const int WorkingDaysPerWeek = 5;

    /// <summary>The team sheet states hours for this many weeks; longer sprints scale the figure up.</summary>
    public const int BaselineWeeks = 2;

    public const int DefaultWeeks = 2;

    /// <summary>The most one person can work in the baseline window (10 days of 8 hours).</summary>
    public const decimal MaxBaselineHours = BaselineWeeks * WorkingDaysPerWeek * WorkingHoursPerDay;

    public static IReadOnlyList<int> AllowedWeeks { get; } = [2, 3, 4];

    public static bool IsValidCadence(int weeks) => AllowedWeeks.Contains(weeks);

    /// <summary>Hours available in one sprint of <paramref name="weeks"/> weeks, given the hours for the baseline window.</summary>
    public static decimal SprintHours(decimal baselineHours, int weeks) => baselineHours * weeks / BaselineWeeks;

    public static decimal DaysToHours(decimal days) => days * WorkingHoursPerDay;
}
