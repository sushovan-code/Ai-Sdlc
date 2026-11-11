using AiSdlc.Application.Abstractions;
using AiSdlc.Domain.Projects;
using NSubstitute;

namespace AiSdlc.UnitTests.Support;

public static class TestProjects
{
    public static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

    /// <summary>A project owned by the manager with the architect as a second member.</summary>
    public static Project WithTeam()
    {
        var project = Project.Create("Express Checkout", "Faster checkout", TestUsers.ManagerId, Now);
        project.AddMember(TestUsers.ArchitectId, Now);
        project.AddMember(TestUsers.DeveloperId, Now);
        return project;
    }

    public static IProjectRepository RepositoryHolding(Project project)
    {
        var repository = Substitute.For<IProjectRepository>();
        repository.GetAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        return repository;
    }

    public static ProjectFile AddRequirements(Project project, int times = 1)
    {
        ProjectFile? last = null;
        for (var i = 0; i < times; i++)
        {
            last = project.AddFile(
                FileKind.Requirements, "reqs.docx", 10, new string('A', 64), $"projects/{project.Id:N}/{Guid.NewGuid():N}", TestUsers.ManagerId, Now);
        }

        return last!;
    }
}
