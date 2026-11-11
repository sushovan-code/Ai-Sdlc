using AiSdlc.Domain.Planning;
using Shouldly;

namespace AiSdlc.UnitTests.Planning;

public sealed class SprintSchedulerTests
{
    [Fact]
    public void Schedule_SingleTask_GoesToTheOnlyPersonInSprintOne()
    {
        var plan = new PlanBuilder().Member("Asha", 40).Task("A", 10);

        var result = plan.Schedule();

        plan.Placement(result, "A").ShouldBe(("Asha", 1));
        result.Unassigned.ShouldBeEmpty();
    }

    [Fact]
    public void Schedule_WhenASprintIsFull_WorkSpillsOverToTheNextSprint()
    {
        var plan = new PlanBuilder().Member("Asha", 40).Task("A", 30).Task("B", 30);

        var result = plan.Schedule();

        plan.Placement(result, "A").Sprint.ShouldBe(1);
        plan.Placement(result, "B").Sprint.ShouldBe(2);
        result.SprintCount.ShouldBe(2);
    }

    [Fact]
    public void Schedule_KeepsSpillingOverUntilEveryTaskIsAssigned()
    {
        var plan = new PlanBuilder().Member("Asha", 40);
        foreach (var name in new[] { "A", "B", "C", "D", "E" })
        {
            plan.Task(name, 30);
        }

        var result = plan.Schedule();

        result.Unassigned.ShouldBeEmpty();
        result.Assignments.Select(a => a.Sprint).ShouldBe([1, 2, 3, 4, 5]);
        result.SprintCount.ShouldBe(5);
    }

    [Fact]
    public void Schedule_UsesEveryonesRoomInASprintBeforeSpilling()
    {
        var plan = new PlanBuilder().Member("Asha", 40).Member("Ravi", 40).Task("A", 30).Task("B", 30).Task("C", 30);

        var result = plan.Schedule();

        result.Assignments.Count(a => a.Sprint == 1).ShouldBe(2);
        plan.Placement(result, "C").Sprint.ShouldBe(2);
    }

    [Theory]
    [InlineData(2, 60, 2)]
    [InlineData(3, 90, 3)]
    [InlineData(4, 120, 4)]
    public void Schedule_LongerSprintsHoldMoreWork_SoFewerSprintsAreNeeded(int weeks, decimal expectedSprintHours, int tasksInFirstSprint)
    {
        var sprintHours = SprintRules.SprintHours(60, weeks);
        var plan = new PlanBuilder().Member("Asha", sprintHours);
        for (var i = 0; i < 4; i++)
        {
            plan.Task($"T{i}", 30);
        }

        var result = plan.Schedule();

        sprintHours.ShouldBe(expectedSprintHours);
        result.Assignments.Count(a => a.Sprint == 1).ShouldBe(Math.Min(tasksInFirstSprint, 4));
        result.Unassigned.ShouldBeEmpty();
    }

    [Fact]
    public void Schedule_FourWeekSprint_FitsWhatTwoWeekSprintsNeedTwoSprintsFor()
    {
        PlanBuilder Build(int weeks) =>
            new PlanBuilder().Member("Asha", SprintRules.SprintHours(60, weeks)).Task("A", 40).Task("B", 40).Task("C", 40);

        Build(2).Schedule().SprintCount.ShouldBe(3);
        Build(4).Schedule().SprintCount.ShouldBe(1);
    }

    [Fact]
    public void Schedule_PlacesTaskFittingExactlyTheRemainingHours()
    {
        var plan = new PlanBuilder().Member("Asha", 40).Task("A", 25).Task("B", 15);

        var result = plan.Schedule();

        plan.Placement(result, "A").Sprint.ShouldBe(1);
        plan.Placement(result, "B").Sprint.ShouldBe(1);
    }

    [Fact]
    public void Schedule_NeverSplitsATaskAcrossPeopleOrSprints()
    {
        var plan = new PlanBuilder().Member("Asha", 20).Member("Ravi", 20).Task("Big", 30);

        var result = plan.Schedule();

        result.Assignments.ShouldBeEmpty();
        plan.UnassignedReasonOf(result, "Big").ShouldBe(UnassignedReason.ExceedsSprintCapacity);
    }

    [Fact]
    public void Schedule_ATaskWaitsForItsDependency_EvenWhenItIsMoreImportant()
    {
        var plan = new PlanBuilder().Member("Asha", 100)
            .Task("Later", 10, TaskPriority.High, dependsOn: ["First"])
            .Task("First", 10, TaskPriority.Low);

        var result = plan.Schedule();

        result.Assignments.Select(a => a.TaskId).ShouldBe([plan.Id("First"), plan.Id("Later")]);
    }

    [Fact]
    public void Schedule_ADependentNeverStartsBeforeItsDependencySprint()
    {
        var plan = new PlanBuilder().Member("Asha", 40).Member("Ravi", 40, "Angular")
            .Task("Fill", 40, TaskPriority.High)
            .Task("Spills", 30, TaskPriority.High)
            .Task("Follow", 5, TaskPriority.Low, dependsOn: ["Spills"]);

        var result = plan.Schedule();

        plan.Placement(result, "Follow").Sprint.ShouldBeGreaterThanOrEqualTo(plan.Placement(result, "Spills").Sprint);
    }

    [Fact]
    public void Schedule_DependencyPushedToALaterSprint_DragsItsDependentsAlong()
    {
        var plan = new PlanBuilder().Member("Asha", 40)
            .Task("A", 35)
            .Task("B", 35)
            .Task("C", 2, dependsOn: ["B"]);

        var result = plan.Schedule();

        plan.Placement(result, "B").Sprint.ShouldBe(2);
        plan.Placement(result, "C").Sprint.ShouldBe(2);
    }

    [Fact]
    public void Schedule_WhenCapacityIsShort_HigherPriorityGetsTheEarlierSprint()
    {
        var plan = new PlanBuilder().Member("Asha", 40)
            .Task("Low", 30, TaskPriority.Low)
            .Task("High", 30, TaskPriority.High)
            .Task("Medium", 30, TaskPriority.Medium);

        var result = plan.Schedule();

        plan.Placement(result, "High").Sprint.ShouldBe(1);
        plan.Placement(result, "Medium").Sprint.ShouldBe(2);
        plan.Placement(result, "Low").Sprint.ShouldBe(3);
    }

    [Fact]
    public void Schedule_PrefersTheBestSkillMatch_EvenWithLessRoomLeft()
    {
        var plan = new PlanBuilder()
            .Member("Generalist", 80, "SQL")
            .Member("Specialist", 40, "C#", "SQL", "Azure")
            .Task("Api", 10, skills: ["C#", "SQL"]);

        var result = plan.Schedule();

        plan.Placement(result, "Api").Member.ShouldBe("Specialist");
    }

    [Fact]
    public void Schedule_SkillMatchingIgnoresCase()
    {
        var plan = new PlanBuilder().Member("Priya", 40, "devops").Task("Pipeline", 10, skills: ["DevOps"]);

        plan.Schedule().Unassigned.ShouldBeEmpty();
    }

    [Fact]
    public void Schedule_FallsBackToAnotherMatchingPerson_WhenTheBestMatchIsFull()
    {
        var plan = new PlanBuilder()
            .Member("Specialist", 40, "C#", "SQL")
            .Member("Partial", 40, "C#")
            .Task("First", 40, TaskPriority.High, ["C#", "SQL"])
            .Task("Second", 10, TaskPriority.Low, ["C#", "SQL"]);

        var result = plan.Schedule();

        plan.Placement(result, "First").ShouldBe(("Specialist", 1));
        plan.Placement(result, "Second").ShouldBe(("Partial", 1));
    }

    [Fact]
    public void Schedule_TaskWithNoRequiredSkills_GoesToThePersonWithMostRoom()
    {
        var plan = new PlanBuilder().Member("Small", 20).Member("Large", 60).Task("Anything", 10);

        var result = plan.Schedule();

        plan.Placement(result, "Anything").Member.ShouldBe("Large");
    }

    [Fact]
    public void Schedule_NobodyHasTheSkill_FlagsTheTask_ButStillPlacesTheRest()
    {
        var plan = new PlanBuilder().Member("Asha", 40, "C#").Task("Mobile", 10, skills: ["Swift"]).Task("Api", 10, skills: ["C#"]);

        var result = plan.Schedule();

        plan.UnassignedReasonOf(result, "Mobile").ShouldBe(UnassignedReason.NoMatchingSkill);
        plan.Placement(result, "Api").Sprint.ShouldBe(1);
    }

    [Fact]
    public void Schedule_ATaskBiggerThanAnyonesSprint_IsFlaggedForTheManagerToSplit()
    {
        var plan = new PlanBuilder().Member("Asha", 40).Task("Huge", 41).Task("Fine", 5);

        var result = plan.Schedule();

        plan.UnassignedReasonOf(result, "Huge").ShouldBe(UnassignedReason.ExceedsSprintCapacity);
        plan.Placement(result, "Fine").Sprint.ShouldBe(1);
    }

    [Fact]
    public void Schedule_WorkThatWaitsForAnUnplacedTask_IsFlaggedToo()
    {
        var plan = new PlanBuilder().Member("Asha", 40).Task("Huge", 99).Task("Child", 5, dependsOn: ["Huge"]);

        var result = plan.Schedule();

        plan.UnassignedReasonOf(result, "Child").ShouldBe(UnassignedReason.DependencyUnassigned);
    }

    [Fact]
    public void Schedule_ADependencyLoop_IsFlagged_WithoutBlockingIndependentWork()
    {
        var plan = new PlanBuilder().Member("Asha", 40)
            .Task("A", 5, dependsOn: ["B"])
            .Task("B", 5, dependsOn: ["A"])
            .Task("Free", 5);

        var result = plan.Schedule();

        plan.UnassignedReasonOf(result, "A").ShouldBe(UnassignedReason.CircularDependency);
        plan.UnassignedReasonOf(result, "B").ShouldBe(UnassignedReason.CircularDependency);
        plan.Placement(result, "Free").Sprint.ShouldBe(1);
    }

    [Fact]
    public void Schedule_ADependencyThatDoesNotExist_IsFlagged()
    {
        var plan = new PlanBuilder().Member("Asha", 40).Task("Orphan", 5);
        var withGhost = plan.Tasks[0] with { DependsOn = [Guid.NewGuid()] };

        var result = SprintScheduler.Schedule([withGhost], plan.Members);

        result.Unassigned.Single().Reason.ShouldBe(UnassignedReason.UnknownDependency);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Schedule_ANonPositiveEstimate_IsFlagged(double hours)
    {
        var plan = new PlanBuilder().Member("Asha", 40).Task("Odd", (decimal)hours);

        plan.UnassignedReasonOf(plan.Schedule(), "Odd").ShouldBe(UnassignedReason.InvalidEstimate);
    }

    [Fact]
    public void Schedule_EveryoneAtZeroHours_MeansNoCapacity()
    {
        var plan = new PlanBuilder().Member("Away", 0).Task("A", 5);

        plan.UnassignedReasonOf(plan.Schedule(), "A").ShouldBe(UnassignedReason.NoCapacity);
    }

    [Fact]
    public void Schedule_NoTeamAtAll_MeansNoCapacity()
    {
        var plan = new PlanBuilder().Task("A", 5);

        plan.UnassignedReasonOf(plan.Schedule(), "A").ShouldBe(UnassignedReason.NoCapacity);
    }

    [Fact]
    public void Schedule_NoTasks_GivesAnEmptyPlan()
    {
        var result = new PlanBuilder().Member("Asha", 40).Schedule();

        result.Assignments.ShouldBeEmpty();
        result.SprintCount.ShouldBe(0);
    }

    [Fact]
    public void Schedule_IsDeterministic_AndBreaksTiesByName()
    {
        PlanBuilder Build() => new PlanBuilder().Member("Zed", 40).Member("Amy", 40).Task("A", 10);

        var first = Build();
        var second = Build();

        first.Placement(first.Schedule(), "A").Member.ShouldBe("Amy");
        second.Placement(second.Schedule(), "A").Member.ShouldBe("Amy");
    }

    [Fact]
    public void Schedule_DuplicateTaskIds_Throw()
    {
        var task = new SchedulableTask(Guid.NewGuid(), "A", 5, TaskPriority.Medium, [], []);

        Should.Throw<ArgumentException>(() => SprintScheduler.Schedule([task, task], []));
    }

    [Fact]
    public void Schedule_SampleTeamAndWork_AssignsEverythingAcrossSprints()
    {
        // Availability from the sample sheet (hours in a 2-week window): Asha 30, Ravi 40, Priya 32, Meera 40, Arjun 30.
        var plan = new PlanBuilder()
            .Member("Asha Rao", 30, "C#", "ASP.NET Core", "SQL", "EF Core")
            .Member("Ravi Kumar", 40, "Angular", "TypeScript", "CSS")
            .Member("Priya Nair", 32, "C#", "Security", "Azure", "DevOps")
            .Member("Meera Iyer", 40, "Testing", "Selenium", "API Testing")
            .Member("Arjun Das", 30, "Testing", "Security Testing")
            .Task("Tokens", 24, TaskPriority.High, ["Security", "C#"])
            .Task("Orchestrator", 28, TaskPriority.High, ["C#", "ASP.NET Core", "SQL"], ["Tokens"])
            .Task("UI", 32, TaskPriority.Medium, ["Angular", "TypeScript"], ["Orchestrator"])
            .Task("ApiTests", 36, TaskPriority.Medium, ["API Testing"], ["Orchestrator"])
            .Task("SecurityTests", 20, TaskPriority.Low, ["Security Testing"], ["Tokens"]);

        var result = plan.Schedule();

        result.Unassigned.ShouldBeEmpty();
        plan.Placement(result, "Tokens").Member.ShouldBe("Priya Nair");
        plan.Placement(result, "Orchestrator").Member.ShouldBe("Asha Rao");
        plan.Placement(result, "UI").Member.ShouldBe("Ravi Kumar");
        plan.Placement(result, "Orchestrator").Sprint.ShouldBeGreaterThanOrEqualTo(plan.Placement(result, "Tokens").Sprint);
    }
}
