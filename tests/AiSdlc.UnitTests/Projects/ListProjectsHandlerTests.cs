using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Application.Projects;
using AiSdlc.Application.Projects.ListProjects;
using AiSdlc.Domain.Projects;
using AiSdlc.UnitTests.Support;
using NSubstitute;
using Shouldly;

namespace AiSdlc.UnitTests.Projects;

public sealed class ListProjectsHandlerTests
{
    private readonly IProjectRepository _projects = Substitute.For<IProjectRepository>();

    [Fact]
    public async Task Handle_AsksOnlyForTheCallersOwnProjects()
    {
        var page = new PagedResult<ProjectSummaryDto>(
            [new ProjectSummaryDto(Guid.NewGuid(), "P", null, ProjectStage.Draft, TestProjects.Now, 2, false, false)], 1, 24, 1);
        _projects.ListForMemberAsync(TestUsers.ArchitectId, 1, 24, Arg.Any<CancellationToken>()).Returns(page);
        var handler = new ListProjectsHandler(new ListProjectsValidator(), TestUsers.Architect(), _projects);

        var result = await handler.HandleAsync(new ListProjects(), CancellationToken.None);

        result.Value.ShouldBe(page);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, ListProjects.MaxPageSize + 1)]
    public async Task Handle_PageOutOfRange_ReturnsValidationError(int page, int pageSize)
    {
        var handler = new ListProjectsHandler(new ListProjectsValidator(), TestUsers.Manager(), _projects);

        var result = await handler.HandleAsync(new ListProjects(page, pageSize), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Validation);
        await _projects.DidNotReceiveWithAnyArgs().ListForMemberAsync(default, default, default, default);
    }
}
