using AiSdlc.Domain.Planning;
using Shouldly;

namespace AiSdlc.UnitTests.Planning;

public sealed class UtilizationAndRulesTests
{
    private static readonly Guid AshaId = Guid.NewGuid();
    private static readonly Guid RaviId = Guid.NewGuid();

    private static readonly SprintMember Asha = new(AshaId, "Asha", [], 40);
    private static readonly SprintMember Ravi = new(RaviId, "Ravi", [], 20);

    [Fact]
    public void Calculate_NothingAssigned_ShowsZeroPercentOverOneSprint()
    {
        var result = UtilizationCalculator.Calculate([Asha], []);

        result.Single().AllocatedHours.ShouldBe(0);
        result.Single().TotalCapacityHours.ShouldBe(40);
        result.Single().Percent.ShouldBe(0);
        result.Single().Sprints.Count.ShouldBe(1);
    }

    [Fact]
    public void Calculate_CountsCapacityOfEverySprintThePlanSpans()
    {
        var assignments = new[]
        {
            new Assignment(Guid.NewGuid(), AshaId, 1, 40),
            new Assignment(Guid.NewGuid(), AshaId, 3, 20),
        };

        var asha = UtilizationCalculator.Calculate([Asha, Ravi], assignments).Single(u => u.MemberId == AshaId);

        asha.AllocatedHours.ShouldBe(60);
        asha.TotalCapacityHours.ShouldBe(120);
        asha.Percent.ShouldBe(50);
        asha.Sprints.Select(s => s.AllocatedHours).ShouldBe([40m, 0m, 20m]);
    }

    [Fact]
    public void Calculate_AMemberWithNoWorkInALongPlan_ShowsAsUnderUsed()
    {
        var assignments = new[] { new Assignment(Guid.NewGuid(), AshaId, 2, 10) };

        var ravi = UtilizationCalculator.Calculate([Asha, Ravi], assignments).Single(u => u.MemberId == RaviId);

        ravi.AllocatedHours.ShouldBe(0);
        ravi.TotalCapacityHours.ShouldBe(40);
        ravi.Percent.ShouldBe(0);
    }

    [Fact]
    public void Calculate_ZeroCapacity_DoesNotDivideByZero()
    {
        var away = new SprintMember(Guid.NewGuid(), "Away", [], 0);

        UtilizationCalculator.Calculate([away], []).Single().Percent.ShouldBe(0);
    }

    [Theory]
    [InlineData(2, 30, 30)]
    [InlineData(3, 30, 45)]
    [InlineData(4, 30, 60)]
    [InlineData(4, 32, 64)]
    [InlineData(3, 0, 0)]
    public void SprintHours_ScalesTheTwoWeekFigureByTheSprintLength(int weeks, decimal baseline, decimal expected)
    {
        SprintRules.SprintHours(baseline, weeks).ShouldBe(expected);
    }

    [Theory]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    [InlineData(1, false)]
    [InlineData(5, false)]
    [InlineData(0, false)]
    [InlineData(-2, false)]
    public void IsValidCadence_AllowsOnlyTwoThreeOrFourWeeks(int weeks, bool expected)
    {
        SprintRules.IsValidCadence(weeks).ShouldBe(expected);
    }

    [Fact]
    public void WorkingTime_IsEightHoursADayAndTwoWeeksByDefault()
    {
        SprintRules.WorkingHoursPerDay.ShouldBe(8);
        SprintRules.DefaultWeeks.ShouldBe(2);
        SprintRules.MaxBaselineHours.ShouldBe(80);
        SprintRules.DaysToHours(2.5m).ShouldBe(20);
    }
}
