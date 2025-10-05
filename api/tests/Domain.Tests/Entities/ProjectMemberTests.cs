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
            var joinedAt = DateTimeOffset.UtcNow.AddDays(-2);
            var removedAt = DateTimeOffset.UtcNow.AddDays(-1);
            var rv = Bytes(8);

            var pm = new ProjectMember(projectId, userId, role, joinedAt)
            {
                RemovedAt = removedAt,
                RowVersion = rv
            };

            pm.ProjectId.Should().Be(projectId);
            pm.UserId.Should().Be(userId);
            pm.Role.Should().Be(role);
            pm.JoinedAt.Should().Be(joinedAt);
            pm.RemovedAt.Should().Be(removedAt);
            pm.RowVersion.Should().BeSameAs(rv);
        }

        [Fact]
        public void Navigation_Properties_Assignable()
        {
            var user = User.Create(Email.Create("member@demo.com"), UserName.Create("Project Member"), Bytes(32), Bytes(16));
            var p = Project.Create(user.Id, ProjectName.Create("A Project Name"), DateTimeOffset.UtcNow.AddDays(-5));

            var pm = new ProjectMember(p.Id, user.Id, ProjectRole.Reader, DateTimeOffset.UtcNow)
            {
                Project = p,
                User = user
            };

            pm.Project.Should().BeSameAs(p);
            pm.User.Should().BeSameAs(user);
            pm.ProjectId.Should().Be(p.Id);
            pm.UserId.Should().Be(user.Id);
        }

        [Fact]
        public void RemovedAt_After_JoinedAt_When_Assigned()
        {
            var joinedAt = DateTimeOffset.UtcNow.AddHours(-2);
            var removedAt = joinedAt.AddMinutes(30);

            var pm = new ProjectMember(Guid.NewGuid(), Guid.NewGuid(), ProjectRole.Member, joinedAt)
            {
                RemovedAt = removedAt
            };

            (pm.RemovedAt >= pm.JoinedAt).Should().BeTrue();
        }

        [Fact]
        public void Role_Can_Change()
        {
            var pm = new ProjectMember(Guid.NewGuid(), Guid.NewGuid(), ProjectRole.Reader, DateTimeOffset.UtcNow);

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
