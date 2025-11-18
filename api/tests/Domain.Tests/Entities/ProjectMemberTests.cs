using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using TestHelpers.Common;
using TestHelpers.Common.Testing;
using TestHelpers.Common.Time;

namespace Domain.Tests.Entities
{
    [UnitTest]
    public class ProjectMemberTests
    {
        private static readonly Guid _defaultProjectId = Guid.NewGuid();
        private static readonly Guid _defaultUserId = Guid.NewGuid();
        private static readonly ProjectRole _defaultRole = ProjectRole.Member;
        private readonly ProjectMember _defaultProjectMember = ProjectMember.Create(
            _defaultProjectId,
            _defaultUserId,
            _defaultRole);

        [Fact]
        public void Set_All_Core_Properties_Assigns_Correctly()
        {
            var projectMember = _defaultProjectMember;
            var removedAt = TestTime.FromFixedMinutes(-10);
            var rowVersion = TestDataFactory.Bytes(8);

            projectMember.Remove(removedAt);
            projectMember.SetRowVersion(rowVersion);

            projectMember.ProjectId.Should().Be(_defaultProjectId);
            projectMember.UserId.Should().Be(_defaultUserId);
            projectMember.Role.Should().Be(_defaultRole);
            projectMember.JoinedAt.Should().NotBe(null);
            projectMember.RemovedAt.Should().Be(removedAt);
            projectMember.RowVersion.Should().BeSameAs(rowVersion);
        }

        [Fact]
        public void Navigation_Properties_Assignable()
        {
            var user = User.Create(
                email: Email.Create("member@demo.com"),
                name: UserName.Create("Project Member"),
                hash: TestDataFactory.Bytes(32),
                salt: TestDataFactory.Bytes(16));
            var project = Project.Create(user.Id, name: ProjectName.Create("A Project Name"));
            var projectMember = ProjectMember.Create(project.Id, user.Id, _defaultRole);

            projectMember.SetUser(user);
            projectMember.SetProject(project);

            projectMember.Project.Should().BeSameAs(project);
            projectMember.User.Should().BeSameAs(user);
            projectMember.ProjectId.Should().Be(project.Id);
            projectMember.UserId.Should().Be(user.Id);
        }

        [Fact]
        public void ChangeRole_Changes_Role()
        {
            var projectMember = _defaultProjectMember;

            projectMember.Role.Should().Be(_defaultRole);

            projectMember.ChangeRole(ProjectRole.Reader);
            projectMember.Role.Should().Be(ProjectRole.Reader);

            projectMember.ChangeRole(ProjectRole.Admin);
            projectMember.Role.Should().Be(ProjectRole.Admin);

            projectMember.ChangeRole(ProjectRole.Owner);
            projectMember.Role.Should().Be(ProjectRole.Owner);
        }

        [Fact]
        public void Restore_Sets_RemovedAt_Null()
        {
            var projectMember = _defaultProjectMember;

            projectMember.RemovedAt.Should().BeNull();

            var removedAt = TestTime.FromFixedMinutes(-5);
            projectMember.Remove(removedAt);

            projectMember.RemovedAt.Should().Be(removedAt);

            projectMember.Restore();
            projectMember.RemovedAt.Should().BeNull();
        }
    }
}
