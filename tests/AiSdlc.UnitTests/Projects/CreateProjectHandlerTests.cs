using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Application.Projects;
using AiSdlc.Application.Projects.CreateProject;
using AiSdlc.Domain.Auditing;
using AiSdlc.Domain.Projects;
using AiSdlc.UnitTests.Support;
using NSubstitute;
using Shouldly;

namespace AiSdlc.UnitTests.Projects;

public sealed class CreateProjectHandlerTests
{
    private readonly IProjectRepository _projects = Substitute.For<IProjectRepository>();
    private readonly IAuditLog _audit = Substitute.For<IAuditLog>();

    private CreateProjectHandler HandlerFor(ICurrentUser caller) =>
        new(new CreateProjectValidator(), caller, _projects, _audit, new FixedTime(TestProjects.Now));

    [Fact]
    public async Task Handle_ProjectManager_CreatesTheProjectWithThemAsOwnerAndAudits()
    {
        Project? saved = null;
        await _projects.AddAsync(Arg.Do<Project>(p => saved = p), Arg.Any<CancellationToken>());

        var result = await HandlerFor(TestUsers.Manager())
            .HandleAsync(new CreateProject("  Express Checkout  ", " Faster checkout "), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Express Checkout");
        result.Value.Description.ShouldBe("Faster checkout");
        result.Value.Stage.ShouldBe(ProjectStage.Draft);
        result.Value.MemberCount.ShouldBe(1);
        saved!.CreatedById.ShouldBe(TestUsers.ManagerId);
        await _projects.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _audit.Received(1).WriteAsync(
            Arg.Is<AuditRecord>(r => r.Action == AuditActions.ProjectCreated && r.ActorId == TestUsers.ManagerId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BlankDescription_IsStoredAsNothing()
    {
        var result = await HandlerFor(TestUsers.Manager())
            .HandleAsync(new CreateProject("Express Checkout", "   "), CancellationToken.None);

        result.Value.Description.ShouldBeNull();
    }

    [Theory]
    [InlineData("Architect")]
    [InlineData("Developer")]
    [InlineData("QA")]
    [InlineData("Admin")]
    public async Task Handle_AnyOtherRole_IsForbidden(string role)
    {
        var result = await HandlerFor(TestUsers.Caller(Guid.NewGuid(), role))
            .HandleAsync(new CreateProject("Express Checkout", null), CancellationToken.None);

        result.Error.Code.ShouldBe(ProjectErrors.ManagerRequiredCode);
        await _projects.DidNotReceiveWithAnyArgs().AddAsync(default!, CancellationToken.None);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public async Task Handle_NameTooShort_ReturnsValidationError(string name)
    {
        var result = await HandlerFor(TestUsers.Manager()).HandleAsync(new CreateProject(name, null), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.ValidationErrors.ShouldNotBeNull().ShouldContainKey("Name");
    }

    [Fact]
    public async Task Handle_NameOrDescriptionTooLong_ReturnsValidationError()
    {
        var tooLongName = new string('n', CreateProjectValidator.MaxNameLength + 1);
        var tooLongDescription = new string('d', CreateProjectValidator.MaxDescriptionLength + 1);

        var result = await HandlerFor(TestUsers.Manager())
            .HandleAsync(new CreateProject(tooLongName, tooLongDescription), CancellationToken.None);

        result.Error.ValidationErrors.ShouldNotBeNull().Keys.ShouldBe(["Name", "Description"], ignoreOrder: true);
    }
}
