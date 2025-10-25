using Domain.Common.Exceptions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using TestHelpers.Time;

namespace Domain.Tests.Entities
{
    public class ProjectTests
    {
        private static readonly ProjectName _defaultProjectName = ProjectName.Create("project");
        private static readonly Guid _defaultOwnerId = Guid.NewGuid();
        private readonly Project _defaultProject = Project.Create(_defaultOwnerId, _defaultProjectName);

        [Fact]
        public void Defaults_Members_Initialized()
        {
            var project = _defaultProject;

            project.Members.Should().NotBeNull();
            project.Members.Count.Should().Be(1);
            project.Members.Single().UserId.Should().Be(_defaultOwnerId);
        }

        [Fact]
        public void Set_All_Core_Properties_Assigns_Correctly()
        {
            var project = _defaultProject;

            project.Id.Should().NotBeEmpty();
            project.OwnerId.Should().Be(_defaultOwnerId);
            project.Name.Should().Be(_defaultProjectName);
            project.Slug.Should().Be(ProjectSlug.Create(_defaultProjectName.Value));
        }

        [Fact]
        public void UpdatedAt_Not_Before_CreatedAt()
        {
            var project = _defaultProject;

            project.UpdatedAt.Should().BeOnOrAfter(project.CreatedAt);
        }

        [Fact]
        public void AddMember_And_RemoveMember_Works_And_Ids_Align()
        {
            var project = _defaultProject;

            project.Members.Should().HaveCount(1);
            project.Members.Single().UserId.Should().Be(_defaultOwnerId);

            var newMemberId = Guid.NewGuid();
            project.AddMember(newMemberId, ProjectRole.Reader);

            project.Members.Should().HaveCount(2);
            project.Members.ElementAt(1).UserId.Should().Be(newMemberId);
            project.Members.ElementAt(1).JoinedAt.Should().NotBe(null);

            var removedAt = TestTime.FromFixedMinutes(30);
            project.RemoveMember(newMemberId, removedAt);

            project.Members.ElementAt(1).RemovedAt.Should().Be(removedAt);
        }

        [Fact]
        public void Add_Existing_Member_Throws()
        {
            var project = _defaultProject;
            var newMemberId = Guid.NewGuid();

            project.AddMember(newMemberId, ProjectRole.Reader);

            var act = () => project.AddMember(newMemberId, ProjectRole.Reader);
            act.Should().Throw<DuplicateEntityException>();

            act = () => project.AddMember(newMemberId, ProjectRole.Member);
            act.Should().Throw<DuplicateEntityException>();
        }

        [Fact]
        public void Add_Owner_Member_Throws()
        {
            var project = _defaultProject;

            var act = () => project.AddMember(userId: Guid.NewGuid(), ProjectRole.Owner);

            act.Should().Throw<DomainRuleViolationException>();
        }

        [Fact]
        public void Remove_Not_Found_Member_Throws()
        {
            var project = _defaultProject;

            var act = () => project.RemoveMember(userId: Guid.NewGuid(), removedAtUtc: TestTime.FixedNow);

            act.Should().Throw<EntityNotFoundException>();
        }

        [Fact]
        public void Remove_Owner_Member_Throws()
        {
            var project = _defaultProject;

            project.AddMember(userId: Guid.NewGuid(), ProjectRole.Reader);

            var act = () => project.RemoveMember(_defaultOwnerId, removedAtUtc: TestTime.FromFixedMinutes(60));
            act.Should().Throw<DomainRuleViolationException>();
        }

        [Fact]
        public void ChangeMemberRole_Works()
        {
            var project = _defaultProject;
            var newMemberId = Guid.NewGuid();

            project.AddMember(newMemberId, ProjectRole.Reader);
            project.Members.ElementAt(1).Role.Should().Be(ProjectRole.Reader);

            project.ChangeMemberRole(newMemberId, ProjectRole.Member);
            project.Members.ElementAt(1).Role.Should().Be(ProjectRole.Member);

            project.ChangeMemberRole(newMemberId, ProjectRole.Admin);
            project.Members.ElementAt(1).Role.Should().Be(ProjectRole.Admin);

            project.ChangeMemberRole(newMemberId, ProjectRole.Member);
            project.Members.ElementAt(1).Role.Should().Be(ProjectRole.Member);

            project.ChangeMemberRole(newMemberId, ProjectRole.Reader);
            project.Members.ElementAt(1).Role.Should().Be(ProjectRole.Reader);
        }

        [Fact]
        public void Change_Not_Found_Member_Role_Throws()
        {
            var project = _defaultProject;
            var act = () => project.ChangeMemberRole(userId: Guid.NewGuid(), ProjectRole.Admin);

            act.Should().Throw<EntityNotFoundException>();
        }

        [Fact]
        public void Change_Owner_Role_Without_Transfering_Ownership_Throws()
        {
            var project = _defaultProject;

            var act = () => project.ChangeMemberRole(_defaultOwnerId, ProjectRole.Member);
            act.Should().Throw<DomainRuleViolationException>();

            project.AddMember(userId: Guid.NewGuid(), ProjectRole.Reader);

            act = () => project.ChangeMemberRole(_defaultOwnerId, ProjectRole.Admin);
            act.Should().Throw<DomainRuleViolationException>();
        }

        [Fact]
        public void ChangeMemberRole_To_Owner_When_There_Is_An_Owner_Throws()
        {
            var project = _defaultProject;
            var newMemberId = Guid.NewGuid();

            project.AddMember(newMemberId, ProjectRole.Reader);

            var act = () => project.ChangeMemberRole(newMemberId, ProjectRole.Owner);
            act.Should().Throw<DomainRuleViolationException>();
        }

        [Fact]
        public void Change_Exowner_Role_After_Transfering_Ownership_Works()
        {
            var project = _defaultProject;
            var newMemberId = Guid.NewGuid();

            project.AddMember(newMemberId, ProjectRole.Reader);
            project.TransferOwnership(newMemberId);
            project.ChangeMemberRole(_defaultOwnerId, ProjectRole.Member);

            project.Members.ElementAt(0).Role.Should().Be(ProjectRole.Member);
        }

        [Fact]
        public void TransferOwnership_Works()
        {
            var project = _defaultProject;
            var newMemberId = Guid.NewGuid();

            project.AddMember(newMemberId, ProjectRole.Reader);
            project.TransferOwnership(newMemberId);

            project.Members.ElementAt(1).Role.Should().Be(ProjectRole.Owner);
        }

        [Fact]
        public void TransferOwnership_To_Inactive_Member_Throws()
        {
            var project = _defaultProject;
            var newMemberId = Guid.NewGuid();

            project.AddMember(newMemberId, ProjectRole.Reader);
            project.RemoveMember(newMemberId, TestTime.FromFixedMinutes(10));

            var act = () => project.TransferOwnership(newMemberId);
            act.Should().Throw<DomainRuleViolationException>();
        }
    }
}
