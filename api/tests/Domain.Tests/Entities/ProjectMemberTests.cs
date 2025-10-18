using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.Entities
{
    public class ProjectMemberTests
    {
        private static byte[] Bytes(int n) => Enumerable.Repeat((byte)0xCD, n).ToArray();

        [Fact]
        public void Set_All_Core_Properties_Assigns_Correctly()
        {
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var role = ProjectRole.Member;
            var removedAt = DateTimeOffset.UtcNow.AddDays(-1);
            var rv = Bytes(8);

            var pm = ProjectMember.Create(projectId, userId, role);
            pm.RemovedAt = removedAt;
            pm.RowVersion = rv;

            pm.ProjectId.Should().Be(projectId);
            pm.UserId.Should().Be(userId);
            pm.Role.Should().Be(role);
            pm.JoinedAt.Should().NotBe(null);
            pm.RemovedAt.Should().Be(removedAt);
            pm.RowVersion.Should().BeSameAs(rv);
        }

        [Fact]
        public void Navigation_Properties_Assignable()
        {
            var user = User.Create(Email.Create("member@demo.com"), UserName.Create("Project Member"), Bytes(32), Bytes(16));
            var p = Project.Create(user.Id, ProjectName.Create("A Project Name"));

            var pm = ProjectMember.Create(p.Id, user.Id, ProjectRole.Reader);
            pm.User = user;
            pm.Project = p;

            pm.Project.Should().BeSameAs(p);
            pm.User.Should().BeSameAs(user);
            pm.ProjectId.Should().Be(p.Id);
            pm.UserId.Should().Be(user.Id);
        }

        [Fact]
        public void Role_Can_Change()
        {
            var pm = ProjectMember.Create(Guid.NewGuid(), Guid.NewGuid(), ProjectRole.Reader);

            pm.Role.Should().Be(ProjectRole.Reader);
            pm.ChangeRole(ProjectRole.Member);
            pm.Role.Should().Be(ProjectRole.Member);
            pm.ChangeRole(ProjectRole.Admin);
            pm.Role.Should().Be(ProjectRole.Admin);
            pm.ChangeRole(ProjectRole.Owner);
            pm.Role.Should().Be(ProjectRole.Owner);
        }
    }
}
