using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.Entities
{
    public class ProjectTests
    {
        private static byte[] Bytes(int n) => Enumerable.Repeat((byte)0xEF, n).ToArray();

        [Fact]
        public void Defaults_Members_Initialized()
        {
            var p = new Project
            {
                Id = Guid.NewGuid(),
                Name = ProjectName.Create("project a"),
                Slug = ProjectSlug.Create("project-a"),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                RowVersion = Bytes(8)
            };

            p.Members.Should().NotBeNull();
            p.Members.Should().BeEmpty();
        }

        [Fact]
        public void Set_All_Core_Properties_Assigns_Correctly()
        {
            var id = Guid.NewGuid();
            var name = ProjectName.Create("kanban board");
            var slug = ProjectSlug.Create("kanban-board");
            var created = DateTimeOffset.UtcNow.AddHours(-6);
            var updated = DateTimeOffset.UtcNow.AddHours(-1);
            var rv = Bytes(16);

            var p = new Project
            {
                Id = id,
                Name = name,
                Slug = slug,
                CreatedAt = created,
                UpdatedAt = updated,
                RowVersion = rv
            };

            p.Id.Should().Be(id);
            p.Name.Should().Be(name);
            p.Slug.Should().Be(slug);
            p.CreatedAt.Should().Be(created);
            p.UpdatedAt.Should().Be(updated);
            p.RowVersion.Should().BeSameAs(rv);
        }

        [Fact]
        public void UpdatedAt_Not_Before_CreatedAt()
        {
            var created = DateTimeOffset.UtcNow.AddDays(-1);
            var updated = created.AddMinutes(5);

            var p = new Project
            {
                Id = Guid.NewGuid(),
                Name = ProjectName.Create("p"),
                Slug = ProjectSlug.Create("p"),
                CreatedAt = created,
                UpdatedAt = updated,
                RowVersion = Bytes(8)
            };

            (p.UpdatedAt >= p.CreatedAt).Should().BeTrue();
        }

        [Fact]
        public void Members_Add_And_Remove_Work_And_Ids_Align()
        {
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var p = new Project
            {
                Id = projectId,
                Name = ProjectName.Create("proj"),
                Slug = ProjectSlug.Create("proj"),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                RowVersion = Bytes(8)
            };

            var m = new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId,
                Role = ProjectRole.Owner,
                JoinedAt = DateTimeOffset.UtcNow
            };

            p.Members.Add(m);

            p.Members.Should().HaveCount(1);
            p.Members.Single().Should().BeSameAs(m);
            p.Members.Single().ProjectId.Should().Be(projectId);

            p.Members.Remove(m);
            p.Members.Should().BeEmpty();
        }
    }
}
