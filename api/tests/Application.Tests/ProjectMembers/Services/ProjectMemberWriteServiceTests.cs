using Application.ProjectMembers.Services;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers.Common;
using TestHelpers.Persistence;

namespace Application.Tests.ProjectMembers.Services
{
    public sealed class ProjectMemberWriteServiceTests
    {
        [Fact]
        public async Task CreateAsync_Returns_Created_When_Not_Existing()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);
            var uow = new UnitOfWork(db);
            var writeSvc = new ProjectMemberWriteService(repo, uow);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);
            var user = TestDataFactory.SeedUser(db);

            var (result, projectMember) = await writeSvc.CreateAsync(projectId, user.Id, ProjectRole.Member);
            result.Should().Be(DomainMutation.Created);
            projectMember.Should().NotBeNull();
        }

        [Fact]
        public async Task ChangeRoleAsync_Returns_Conflict_When_RowVersion_Mismatch()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);
            var uow = new UnitOfWork(db);
            var writeSvc = new ProjectMemberWriteService(repo, uow);

            var (projectId, uId) = TestDataFactory.SeedUserWithProject(db);
            var result = await writeSvc.ChangeRoleAsync(projectId, uId, ProjectRole.Admin, [1, 2, 3, 4]);

            result.Should().Be(DomainMutation.Conflict);
        }

        [Fact]
        public async Task RemoveAsync_Then_Restore_Workflow()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);
            var uow = new UnitOfWork(db);
            var writeSvc = new ProjectMemberWriteService(repo, uow);

            var (projectId, uId) = TestDataFactory.SeedUserWithProject(db);
            var member = await db.ProjectMembers.SingleAsync(m => m.ProjectId == projectId && m.UserId == uId);

            var removeResult = await writeSvc.RemoveAsync(member.ProjectId, member.UserId, member.RowVersion);
            removeResult.Should().Be(DomainMutation.Updated);

            var removed = await db.ProjectMembers.SingleAsync(m => m.ProjectId == projectId && m.UserId == uId);
            member.RemovedAt.Should().NotBeNull();

            var restoreResult = await writeSvc.RestoreAsync(member.ProjectId, member.UserId, removed.RowVersion);
            restoreResult.Should().Be(DomainMutation.Updated);
            member.RemovedAt.Should().BeNull();
        }
    }
}
