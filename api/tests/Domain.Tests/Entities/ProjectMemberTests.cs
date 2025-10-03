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
            var role = ProjectRole.Editor;
            var joinedAt = DateTimeOffset.UtcNow.AddDays(-2);
            var invitedAt = DateTime.UtcNow.AddDays(-3);
            var removedAt = DateTimeOffset.UtcNow.AddDays(-1);
            var rv = Bytes(8);

            var pm = new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId,
                Role = role,
                JoinedAt = joinedAt,
                InvitedAt = invitedAt,
                RemovedAt = removedAt,
                RowVersion = rv
            };

            pm.ProjectId.Should().Be(projectId);
            pm.UserId.Should().Be(userId);
            pm.Role.Should().Be(role);
            pm.JoinedAt.Should().Be(joinedAt);
            pm.InvitedAt.Should().Be(invitedAt);
            pm.RemovedAt.Should().Be(removedAt);
            pm.RowVersion.Should().BeSameAs(rv);
        }

        [Fact]
        public void Navigation_Properties_Assignable()
        {
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = ProjectName.Create("proj"),
                Slug = ProjectSlug.Create("proj"),
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-5),
                UpdatedAt = DateTimeOffset.UtcNow.AddDays(-4),
                RowVersion = Bytes(4)
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = Email.Create("member@demo.com"),
                Name = UserName.Create("Project Member"),
                PasswordHash = Bytes(32),
                PasswordSalt = Bytes(16),
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-10),
                UpdatedAt = DateTimeOffset.UtcNow.AddDays(-9)
            };

            var pm = new ProjectMember
            {
                ProjectId = project.Id,
                UserId = user.Id,
                Role = ProjectRole.Reader,
                JoinedAt = DateTimeOffset.UtcNow,
                Project = project,
                User = user
            };

            pm.Project.Should().BeSameAs(project);
            pm.User.Should().BeSameAs(user);
            pm.ProjectId.Should().Be(project.Id);
            pm.UserId.Should().Be(user.Id);
        }

        [Fact]
        public void RemovedAt_After_JoinedAt_When_Assigned()
        {
            var joined = DateTimeOffset.UtcNow.AddHours(-2);
            var removed = joined.AddMinutes(30);

            var pm = new ProjectMember
            {
                ProjectId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Role = ProjectRole.Owner,
                JoinedAt = joined,
                RemovedAt = removed
            };

            (pm.RemovedAt >= pm.JoinedAt).Should().BeTrue();
        }

        [Fact]
        public void Role_Can_Change()
        {
            var pm = new ProjectMember
            {
                ProjectId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Role = ProjectRole.Reader,
                JoinedAt = DateTimeOffset.UtcNow
            };

            pm.Role.Should().Be(ProjectRole.Reader);
            pm.Role = ProjectRole.Editor;
            pm.Role.Should().Be(ProjectRole.Editor);
        }
    }
}
