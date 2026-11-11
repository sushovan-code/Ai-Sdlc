using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Application.Team.Import;
using Shouldly;

namespace AiSdlc.UnitTests.Planning;

public sealed class TeamSheetValidatorTests
{
    private static RawTeamRow Row(
        int number = 2,
        string? name = "Asha Rao",
        string? role = "Developer",
        string? skills = "C#, ASP.NET Core, SQL, EF Core",
        string? capacity = "40",
        string? availability = "30") => new(number, name, role, skills, capacity, availability);

    private static string[] Problems(params RawTeamRow[] rows)
    {
        var result = TeamSheetValidator.Validate(rows);
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        return result.Error.ValidationErrors!["file"];
    }

    [Fact]
    public void Validate_TheSampleSheet_ReadsEveryone()
    {
        var rows = new[]
        {
            Row(2, "Asha Rao", "Developer", "C#, ASP.NET Core, SQL, EF Core", "40", "30"),
            Row(3, "Ravi Kumar", "Developer", "Angular, TypeScript, CSS", "40", "40"),
            Row(4, "Priya Nair", "Developer", "C#, Security, Azure, DevOps", "32", "32"),
            Row(5, "Meera Iyer", "QA", "Testing, Selenium, API Testing", "40", "40"),
            Row(6, "Arjun Das", "QA", "Testing, Security Testing", "30", "30"),
        };

        var result = TeamSheetValidator.Validate(rows);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Select(p => p.Name).ShouldBe(["Asha Rao", "Ravi Kumar", "Priya Nair", "Meera Iyer", "Arjun Das"]);
        result.Value[0].Skills.ShouldBe(["C#", "ASP.NET Core", "SQL", "EF Core"]);
        result.Value[0].CapacityHours.ShouldBe(40);
        result.Value[0].AvailabilityHours.ShouldBe(30);
    }

    [Fact]
    public void Validate_TrimsTextAndAcceptsSemicolonsAndDuplicateSkills()
    {
        var result = TeamSheetValidator.Validate([Row(name: "  Asha  ", skills: "C#; c# ,SQL ;; ")]);

        result.Value.Single().Name.ShouldBe("Asha");
        result.Value.Single().Skills.ShouldBe(["C#", "SQL"]);
    }

    [Fact]
    public void Validate_AcceptsHalfHours_AndZeroAvailability()
    {
        var result = TeamSheetValidator.Validate([Row(capacity: "37.5", availability: "0")]);

        result.Value.Single().CapacityHours.ShouldBe(37.5m);
        result.Value.Single().AvailabilityHours.ShouldBe(0);
    }

    [Fact]
    public void Validate_AvailabilityAboveCapacity_NamesTheRowAndBothFigures()
    {
        var problems = Problems(Row(7, capacity: "30", availability: "35"));

        problems.ShouldHaveSingleItem().ShouldBe("Row 7: Availability (35) cannot be more than Capacity (30).");
    }

    [Fact]
    public void Validate_CapacityAboveTenWorkingDays_IsRefused()
    {
        var problems = Problems(Row(capacity: "81", availability: "40"));

        problems.ShouldHaveSingleItem().ShouldContain("more than 80 hours");
    }

    [Fact]
    public void Validate_CapacityOfExactlyTenWorkingDays_IsAllowed()
    {
        TeamSheetValidator.Validate([Row(capacity: "80", availability: "80")]).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-5")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Validate_CapacityThatIsNotAPositiveNumber_IsRefused(string? capacity)
    {
        var problems = Problems(Row(capacity: capacity, availability: "0"));

        problems.ShouldContain(p => p.Contains("Capacity", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null, "Name is empty.")]
    [InlineData("", "Name is empty.")]
    public void Validate_MissingName_IsRefused(string? name, string expected)
    {
        Problems(Row(name: name)).ShouldContain($"Row 2: {expected}");
    }

    [Fact]
    public void Validate_MissingRoleOrSkills_IsRefused()
    {
        var problems = Problems(Row(role: null, skills: " "));

        problems.ShouldContain("Row 2: Role is empty.");
        problems.ShouldContain("Row 2: Skills is empty.");
    }

    [Fact]
    public void Validate_TheSamePersonTwice_IsRefused_IgnoringCase()
    {
        var problems = Problems(Row(2, "Asha Rao"), Row(3, "ASHA RAO"));

        problems.ShouldHaveSingleItem().ShouldBe("Row 3: ASHA RAO appears more than once.");
    }

    [Fact]
    public void Validate_ARepeatedNameIsCaught_EvenWhenTheFirstRowHasOtherProblems()
    {
        var problems = Problems(Row(2, "Asha Rao", capacity: "30", availability: "35"), Row(3, "Asha Rao"));

        problems.ShouldContain("Row 3: Asha Rao appears more than once.");
    }

    [Fact]
    public void Validate_NoPeople_IsRefused()
    {
        TeamSheetValidator.Validate([]).Error.ValidationErrors!["file"].ShouldHaveSingleItem()
            .ShouldBe("The team sheet has no people in it.");
    }

    [Fact]
    public void Validate_TooManyPeople_IsRefused()
    {
        var rows = Enumerable.Range(0, TeamSheetRules.MaxPeople + 1).Select(i => Row(i + 2, $"Person {i}")).ToArray();

        Problems(rows).ShouldHaveSingleItem().ShouldContain("the limit is 500");
    }

    [Fact]
    public void Validate_ListsEveryProblemInOnePass_UpToAReportLimit()
    {
        var rows = Enumerable.Range(0, 15).Select(i => Row(i + 2, $"Person {i}", capacity: "x")).ToArray();

        var problems = Problems(rows);

        problems.Length.ShouldBe(TeamSheetRules.MaxProblemsReported + 1);
        problems[^1].ShouldBe("...and 5 more problem(s).");
    }

    [Fact]
    public void Validate_AGoodRowIsNotAcceptedWhenAnotherRowIsBad()
    {
        var result = TeamSheetValidator.Validate([Row(2, "Good"), Row(3, "Bad", capacity: "x")]);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Validate_OverlongNameRoleOrSkill_IsRefused()
    {
        var problems = Problems(Row(name: new string('n', 101), role: new string('r', 51), skills: new string('s', 51)));

        problems.Length.ShouldBe(3);
    }
}
