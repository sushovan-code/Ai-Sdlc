using AiSdlc.Domain.Projects;
using AiSdlc.Domain.Security;
using AiSdlc.UnitTests.Support;
using Shouldly;

namespace AiSdlc.UnitTests.Projects;

public sealed class ProjectTests
{
    private static readonly DateTimeOffset Now = TestProjects.Now;

    private static ProjectFile AddFile(Project project, FileKind kind) =>
        project.AddFile(kind, "file.bin", 1, new string('B', 64), $"k/{Guid.NewGuid():N}", TestUsers.ManagerId, Now);

    [Fact]
    public void Create_MakesTheCreatorTheFirstMemberAndStartsInDraft()
    {
        var project = Project.Create("Express Checkout", null, TestUsers.ManagerId, Now);

        project.Stage.ShouldBe(ProjectStage.Draft);
        project.CreatedById.ShouldBe(TestUsers.ManagerId);
        project.IsMember(TestUsers.ManagerId).ShouldBeTrue();
        project.Members.Count.ShouldBe(1);
    }

    [Fact]
    public void Create_BlankName_Throws()
    {
        Should.Throw<ArgumentException>(() => Project.Create("  ", null, TestUsers.ManagerId, Now));
    }

    [Fact]
    public void AddMember_NewUser_ReturnsTrueAndGrantsAccess()
    {
        var project = TestProjects.WithTeam();

        project.AddMember(TestUsers.OutsiderId, Now).ShouldBeTrue();

        project.IsMember(TestUsers.OutsiderId).ShouldBeTrue();
    }

    [Fact]
    public void AddMember_ExistingUser_ReturnsFalseAndDoesNotDuplicate()
    {
        var project = TestProjects.WithTeam();

        project.AddMember(TestUsers.ArchitectId, Now).ShouldBeFalse();

        project.Members.Count(m => m.UserId == TestUsers.ArchitectId).ShouldBe(1);
    }

    [Fact]
    public void RemoveMember_Owner_IsRefused()
    {
        var project = TestProjects.WithTeam();

        project.RemoveMember(TestUsers.ManagerId).ShouldBe(MemberRemoval.IsOwner);

        project.IsMember(TestUsers.ManagerId).ShouldBeTrue();
    }

    [Fact]
    public void RemoveMember_UnknownUser_ReportsNotAMember()
    {
        TestProjects.WithTeam().RemoveMember(TestUsers.OutsiderId).ShouldBe(MemberRemoval.NotAMember);
    }

    [Fact]
    public void RemoveMember_Member_RemovesAccess()
    {
        var project = TestProjects.WithTeam();

        project.RemoveMember(TestUsers.ArchitectId).ShouldBe(MemberRemoval.Removed);

        project.IsMember(TestUsers.ArchitectId).ShouldBeFalse();
    }

    [Fact]
    public void AddFile_NumbersVersionsPerKind()
    {
        var project = TestProjects.WithTeam();

        AddFile(project, FileKind.Requirements).Version.ShouldBe(1);
        AddFile(project, FileKind.Requirements).Version.ShouldBe(2);
        AddFile(project, FileKind.TeamTemplate).Version.ShouldBe(1);
    }

    [Fact]
    public void AddFile_AfterDeletingTheLatest_DoesNotReuseTheVersionNumber()
    {
        var project = TestProjects.WithTeam();
        AddFile(project, FileKind.Requirements);
        var second = AddFile(project, FileKind.Requirements);
        project.DeleteFile(second.Id, TestUsers.ManagerId, Now);

        AddFile(project, FileKind.Requirements).Version.ShouldBe(3);
    }

    [Fact]
    public void LatestFile_IgnoresDeletedVersions()
    {
        var project = TestProjects.WithTeam();
        var first = AddFile(project, FileKind.Requirements);
        var second = AddFile(project, FileKind.Requirements);

        project.DeleteFile(second.Id, TestUsers.ManagerId, Now);

        project.LatestFile(FileKind.Requirements)!.Id.ShouldBe(first.Id);
    }

    [Fact]
    public void Stage_BecomesFilesUploaded_OnlyWhenBothKindsArePresent()
    {
        var project = TestProjects.WithTeam();

        AddFile(project, FileKind.Requirements);
        project.Stage.ShouldBe(ProjectStage.Draft);

        AddFile(project, FileKind.TeamTemplate);
        project.Stage.ShouldBe(ProjectStage.FilesUploaded);
    }

    [Fact]
    public void Stage_ReturnsToDraft_WhenAnInputIsDeleted()
    {
        var project = TestProjects.WithTeam();
        AddFile(project, FileKind.Requirements);
        var team = AddFile(project, FileKind.TeamTemplate);

        project.DeleteFile(team.Id, TestUsers.ManagerId, Now);

        project.Stage.ShouldBe(ProjectStage.Draft);
    }

    [Fact]
    public void DeleteFile_MarksWhoAndWhen_AndKeepsTheRow()
    {
        var project = TestProjects.WithTeam();
        var file = AddFile(project, FileKind.Requirements);

        var deleted = project.DeleteFile(file.Id, TestUsers.ManagerId, Now);

        deleted.ShouldNotBeNull();
        deleted.IsDeleted.ShouldBeTrue();
        deleted.DeletedById.ShouldBe(TestUsers.ManagerId);
        deleted.DeletedAtUtc.ShouldBe(Now);
        project.Files.ShouldContain(file);
    }

    [Fact]
    public void DeleteFile_AlreadyDeletedOrUnknown_ReturnsNull()
    {
        var project = TestProjects.WithTeam();
        var file = AddFile(project, FileKind.Requirements);
        project.DeleteFile(file.Id, TestUsers.ManagerId, Now);

        project.DeleteFile(file.Id, TestUsers.ManagerId, Now).ShouldBeNull();
        project.DeleteFile(Guid.NewGuid(), TestUsers.ManagerId, Now).ShouldBeNull();
    }

    [Theory]
    [InlineData(AppRoles.ProjectManager, true)]
    [InlineData(AppRoles.Architect, false)]
    [InlineData(AppRoles.Developer, false)]
    [InlineData(AppRoles.QA, false)]
    [InlineData(AppRoles.Admin, false)]
    [InlineData(null, false)]
    public void CanManage_IsReservedForProjectManagers(string? role, bool expected)
    {
        ProjectPermissions.CanManage(role).ShouldBe(expected);
    }
}
