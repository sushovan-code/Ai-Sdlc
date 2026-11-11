using System.Globalization;
using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Domain.Planning;
using AiSdlc.Domain.Projects;

namespace AiSdlc.Application.Team.Import;

/// <summary>
/// Checks every row of a team sheet and turns it into people. All problems are collected (up to a limit) so the
/// uploader can fix the sheet in one go. Capacity and Availability are hours for a 2-week window.
/// </summary>
public static class TeamSheetValidator
{
    public const string InvalidCode = "team-sheet.invalid";

    private static readonly char[] SkillSeparators = [',', ';'];

    public static Result<IReadOnlyList<TeamMemberDraft>> Validate(IReadOnlyList<RawTeamRow> rows)
    {
        var problems = new List<string>();
        var people = new List<TeamMemberDraft>();

        if (rows.Count == 0)
        {
            problems.Add("The team sheet has no people in it.");
        }
        else if (rows.Count > TeamSheetRules.MaxPeople)
        {
            problems.Add($"The team sheet has {rows.Count} people; the limit is {TeamSheetRules.MaxPeople}.");
        }
        else
        {
            ValidateRows(rows, people, problems);
        }

        return problems.Count == 0
            ? Result<IReadOnlyList<TeamMemberDraft>>.Success(people)
            : Result<IReadOnlyList<TeamMemberDraft>>.Failure(Reject(problems));
    }

    private static void ValidateRows(IReadOnlyList<RawTeamRow> rows, List<TeamMemberDraft> people, List<string> problems)
    {
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var rowProblems = new List<string>();
            var draft = ReadRow(row, rowProblems);

            // Names are compared as written, so a repeat is caught even when the first row also has other problems.
            var written = row.Name?.Trim();
            if (!string.IsNullOrEmpty(written) && !seenNames.Add(written))
            {
                rowProblems.Add($"{written} appears more than once.");
            }

            problems.AddRange(rowProblems.Select(p => $"Row {row.RowNumber}: {p}"));
            if (rowProblems.Count == 0 && draft is not null)
            {
                people.Add(draft);
            }
        }
    }

    private static TeamMemberDraft? ReadRow(RawTeamRow row, List<string> problems)
    {
        var name = Required(row.Name, TeamSheetRules.NameColumn, TeamSheetRules.MaxNameLength, problems);
        var role = Required(row.Role, TeamSheetRules.RoleColumn, TeamSheetRules.MaxRoleLength, problems);
        var skills = ReadSkills(row.Skills, problems);
        var capacity = ReadHours(row.Capacity, TeamSheetRules.CapacityColumn, problems);
        var availability = ReadHours(row.Availability, TeamSheetRules.AvailabilityColumn, problems);

        if (capacity > SprintRules.MaxBaselineHours)
        {
            problems.Add(
                $"Capacity ({capacity}) is more than {SprintRules.MaxBaselineHours} hours, " +
                $"the most one person can work in {SprintRules.BaselineWeeks} weeks.");
        }

        if (availability > capacity)
        {
            problems.Add($"Availability ({availability}) cannot be more than Capacity ({capacity}).");
        }

        return problems.Count > 0 || name is null || role is null || capacity is null || availability is null
            ? null
            : new TeamMemberDraft(name, role, skills, capacity.Value, availability.Value);
    }

    private static string? Required(string? value, string column, int maxLength, List<string> problems)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            problems.Add($"{column} is empty.");
            return null;
        }

        if (text.Length > maxLength)
        {
            problems.Add($"{column} is longer than {maxLength} characters.");
            return null;
        }

        return text;
    }

    private static List<string> ReadSkills(string? value, List<string> problems)
    {
        var skills = (value ?? string.Empty)
            .Split(SkillSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (skills.Count == 0)
        {
            problems.Add($"{TeamSheetRules.SkillsColumn} is empty.");
        }
        else if (skills.Count > TeamSheetRules.MaxSkills)
        {
            problems.Add($"{TeamSheetRules.SkillsColumn} lists more than {TeamSheetRules.MaxSkills} skills.");
        }
        else if (skills.Any(s => s.Length > TeamSheetRules.MaxSkillLength))
        {
            problems.Add($"A skill is longer than {TeamSheetRules.MaxSkillLength} characters.");
        }

        return skills;
    }

    private static decimal? ReadHours(string? value, string column, List<string> problems)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            problems.Add($"{column} is empty.");
            return null;
        }

        var text = value.Trim();
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var hours) || hours < 0)
        {
            problems.Add($"{column} must be a number of hours, zero or more (found '{text}').");
            return null;
        }

        return hours;
    }

    private static Error Reject(List<string> problems)
    {
        var shown = problems.Take(TeamSheetRules.MaxProblemsReported).ToList();
        if (problems.Count > shown.Count)
        {
            shown.Add($"...and {problems.Count - shown.Count} more problem(s).");
        }

        return new Error(InvalidCode, "The team sheet has problems.", ErrorType.Validation)
        {
            ValidationErrors = new Dictionary<string, string[]> { ["file"] = [.. shown] },
        };
    }
}
