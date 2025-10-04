using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.Entities
{
    public class ProjectTests
    {

        [Fact]
        public void Defaults_Members_Initialized()
        {
            var ownerId = Guid.NewGuid();
            var utcNow = DateTimeOffset.UtcNow;
            var projectName = ProjectName.Create("A Project");
            var p = Project.Create(ownerId, projectName, utcNow);

            p.Members.Should().NotBeNull();
            p.Members.Count.Should().Be(1);
            p.Members.Single().UserId.Should().Be(ownerId);
        }

        [Fact]
        public void Set_All_Core_Properties_Assigns_Correctly()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var ownerId = Guid.NewGuid();
            var name = ProjectName.Create("kanban board");
            var slug = ProjectSlug.Create(name);

            var p = Project.Create(ownerId, name, utcNow);

            p.Id.Should().NotBeEmpty();
            p.OwnerId.Should().Be(ownerId);
            p.Name.Should().Be(name);
            p.Slug.Should().Be(slug);
        }

        [Fact]
        public void UpdatedAt_Not_Before_CreatedAt()
        {
            var p = Project.Create(Guid.NewGuid(), ProjectName.Create("Project Name"), DateTimeOffset.UtcNow);

            (p.UpdatedAt >= p.CreatedAt).Should().BeTrue();
        }

        [Fact]
        public void AddMember_Works_And_Ids_Align()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var ownerId = Guid.NewGuid();

            var p = Project.Create(ownerId, ProjectName.Create("Project Name"), utcNow);
            p.Members.Should().HaveCount(1);
            p.Members.Single().UserId.Should().Be(ownerId);

            var newMemberId = Guid.NewGuid();
            p.AddMember(newMemberId, ProjectRole.Reader, utcNow.AddHours(1));
            p.Members.Should().HaveCount(2);
            p.Members.ElementAt(1).UserId.Should().Be(newMemberId);
        }
    }
}
