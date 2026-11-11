using AiSdlc.Application.Analysis;
using Shouldly;

namespace AiSdlc.UnitTests.Analysis;

public sealed class AnalysisGenerationLockTests
{
    [Fact]
    public void TryEnter_SecondTimeForTheSameProject_IsRefusedUntilTheFirstIsReleased()
    {
        var gate = new AnalysisGenerationLock();
        var project = Guid.NewGuid();

        using (gate.TryEnter(project).ShouldNotBeNull())
        {
            gate.IsRunning(project).ShouldBeTrue();
            gate.TryEnter(project).ShouldBeNull();
        }

        gate.IsRunning(project).ShouldBeFalse();
        gate.TryEnter(project).ShouldNotBeNull();
    }

    [Fact]
    public void TryEnter_DifferentProjects_DoNotBlockEachOther()
    {
        var gate = new AnalysisGenerationLock();

        using var first = gate.TryEnter(Guid.NewGuid());
        using var second = gate.TryEnter(Guid.NewGuid());

        first.ShouldNotBeNull();
        second.ShouldNotBeNull();
    }
}
