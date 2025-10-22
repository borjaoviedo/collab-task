using Domain.Common.Exceptions;
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
            var projectName = ProjectName.Create("A Project");
            var p = Project.Create(ownerId, projectName);

            p.Members.Should().NotBeNull();
            p.Members.Count.Should().Be(1);
            p.Members.Single().UserId.Should().Be(ownerId);
        }

        [Fact]
        public void Set_All_Core_Properties_Assigns_Correctly()
        {
            var ownerId = Guid.NewGuid();
            var name = ProjectName.Create("kanban board");
            var slug = ProjectSlug.Create(name);

            var p = Project.Create(ownerId, name);

            p.Id.Should().NotBeEmpty();
            p.OwnerId.Should().Be(ownerId);
            p.Name.Should().Be(name);
            p.Slug.Should().Be(slug);
        }

        [Fact]
        public void UpdatedAt_Not_Before_CreatedAt()
        {
            var p = Project.Create(Guid.NewGuid(), ProjectName.Create("Project Name"));

            (p.UpdatedAt >= p.CreatedAt).Should().BeTrue();
        }

        [Fact]
        public void AddMember_And_RemoveMember_Works_And_Ids_Align()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var ownerId = Guid.NewGuid();

            var p = Project.Create(ownerId, ProjectName.Create("Project Name"));
            p.Members.Should().HaveCount(1);
            p.Members.Single().UserId.Should().Be(ownerId);

            var newMemberId = Guid.NewGuid();
            p.AddMember(newMemberId, ProjectRole.Reader);
            p.Members.Should().HaveCount(2);
            p.Members.ElementAt(1).UserId.Should().Be(newMemberId);
            p.Members.ElementAt(1).JoinedAt.Should().NotBe(null);

            var removedAt = utcNow.AddHours(2);
            p.RemoveMember(newMemberId, removedAt);
            p.Members.ElementAt(1).RemovedAt.Should().Be(removedAt);
        }

        [Fact]
        public void Add_Existing_Member_Throws()
        {
            var p = Project.Create(Guid.NewGuid(), ProjectName.Create("Project Name"));

            var newMemberId = Guid.NewGuid();
            p.AddMember(newMemberId, ProjectRole.Reader);
            Assert.Throws<DuplicateEntityException>(() => p.AddMember(newMemberId, ProjectRole.Reader));
            Assert.Throws<DuplicateEntityException>(() => p.AddMember(newMemberId, ProjectRole.Member));
        }

        [Fact]
        public void Add_Owner_Member_Throws()
        {
            var p = Project.Create(Guid.NewGuid(), ProjectName.Create("Project Name"));
            Assert.Throws<DomainRuleViolationException>(() => p.AddMember(Guid.NewGuid(), ProjectRole.Owner));
        }

        [Fact]
        public void Remove_Not_Found_Member_Throws()
        {
            var utcNow = DateTimeOffset.UtcNow;

            var p = Project.Create(Guid.NewGuid(), ProjectName.Create("Project Name"));
            Assert.Throws<EntityNotFoundException>(() => p.RemoveMember(Guid.NewGuid(), utcNow));
        }

        [Fact]
        public void Remove_Owner_Member_Throws()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var ownerId = Guid.NewGuid();

            var p = Project.Create(ownerId, ProjectName.Create("Project Name"));

            p.AddMember(Guid.NewGuid(), ProjectRole.Reader);
            Assert.Throws<DomainRuleViolationException>(() => p.RemoveMember(ownerId, utcNow.AddHours(2)));
        }

        [Fact]
        public void ChangeMemberRole_Works()
        {
            var p = Project.Create(Guid.NewGuid(), ProjectName.Create("Project Name"));

            var newMemberId = Guid.NewGuid();
            p.AddMember(newMemberId, ProjectRole.Reader);
            p.Members.ElementAt(1).Role.Should().Be(ProjectRole.Reader);
            p.ChangeMemberRole(newMemberId, ProjectRole.Member);
            p.Members.ElementAt(1).Role.Should().Be(ProjectRole.Member);
            p.ChangeMemberRole(newMemberId, ProjectRole.Admin);
            p.Members.ElementAt(1).Role.Should().Be(ProjectRole.Admin);
            p.ChangeMemberRole(newMemberId, ProjectRole.Member);
            p.Members.ElementAt(1).Role.Should().Be(ProjectRole.Member);
            p.ChangeMemberRole(newMemberId, ProjectRole.Reader);
            p.Members.ElementAt(1).Role.Should().Be(ProjectRole.Reader);
        }

        [Fact]
        public void Change_Not_Found_Member_Role_Throws()
        {
            var p = Project.Create(Guid.NewGuid(), ProjectName.Create("Project Name"));

            Assert.Throws<EntityNotFoundException>(() => p.ChangeMemberRole(Guid.NewGuid(), ProjectRole.Admin));
        }

        [Fact]
        public void Change_Owner_Role_Without_Transfering_Ownership_Throws()
        {
            var ownerId = Guid.NewGuid();

            var p = Project.Create(ownerId, ProjectName.Create("Project Name"));
            Assert.Throws<DomainRuleViolationException>(() => p.ChangeMemberRole(ownerId, ProjectRole.Member));

            p.AddMember(Guid.NewGuid(), ProjectRole.Reader);
            Assert.Throws<DomainRuleViolationException>(() => p.ChangeMemberRole(ownerId, ProjectRole.Admin));
        }

        [Fact]
        public void ChangeMemberRole_To_Owner_When_There_Is_An_Owner_Throws()
        {
            var p = Project.Create(Guid.NewGuid(), ProjectName.Create("Project Name"));

            var newMemberId = Guid.NewGuid();
            p.AddMember(newMemberId, ProjectRole.Reader);

            Assert.Throws<DomainRuleViolationException>(() => p.ChangeMemberRole(newMemberId, ProjectRole.Owner));
        }

        [Fact]
        public void Change_Exowner_Role_After_Transfering_Ownership_Works()
        {
            var ownerId = Guid.NewGuid();

            var p = Project.Create(ownerId, ProjectName.Create("Project Name"));

            var newMemberId = Guid.NewGuid();
            p.AddMember(newMemberId, ProjectRole.Reader);

            p.TransferOwnership(newMemberId);
            p.ChangeMemberRole(ownerId, ProjectRole.Member);
            p.Members.ElementAt(0).Role.Should().Be(ProjectRole.Member);
        }

        [Fact]
        public void TransferOwnership_Works()
        {
            var ownerId = Guid.NewGuid();

            var p = Project.Create(ownerId, ProjectName.Create("Project Name"));

            var newMemberId = Guid.NewGuid();
            p.AddMember(newMemberId, ProjectRole.Reader);

            p.TransferOwnership(newMemberId);
            p.Members.ElementAt(1).Role.Should().Be(ProjectRole.Owner);
        }

        [Fact]
        public void TransferOwnership_To_Inactive_Member_Throws()
        {
            var ownerId = Guid.NewGuid();

            var p = Project.Create(ownerId, ProjectName.Create("Project Name"));

            var newMemberId = Guid.NewGuid();
            p.AddMember(newMemberId, ProjectRole.Reader);

            p.RemoveMember(newMemberId, DateTimeOffset.UtcNow.AddHours(1));
            Assert.Throws<DomainRuleViolationException>(() => p.TransferOwnership(newMemberId));
        }
    }
}
