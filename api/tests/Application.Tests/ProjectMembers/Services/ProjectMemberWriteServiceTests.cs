using Application.ProjectMembers.Services;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers;

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
            var svc = new ProjectMemberWriteService(repo, uow);

            var (pId, _) = TestDataFactory.SeedUserWithProject(db);
            var user = TestDataFactory.SeedUser(db);

            var (res, projectMember) = await svc.CreateAsync(pId, user.Id, ProjectRole.Member);
            res.Should().Be(DomainMutation.Created);
            projectMember.Should().NotBeNull();
        }

        [Fact]
        public async Task ChangeRoleAsync_Returns_Conflict_When_RowVersion_Mismatch()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);
            var uow = new UnitOfWork(db);
            var svc = new ProjectMemberWriteService(repo, uow);

            var (pId, uId) = TestDataFactory.SeedUserWithProject(db);
            var res = await svc.ChangeRoleAsync(pId, uId, ProjectRole.Admin, [1, 2, 3, 4]);

            res.Should().Be(DomainMutation.Conflict);
        }

        [Fact]
        public async Task RemoveAsync_Then_Restore_Workflow()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);
            var uow = new UnitOfWork(db);
            var svc = new ProjectMemberWriteService(repo, uow);

            var (pId, uId) = TestDataFactory.SeedUserWithProject(db);
            var member = await db.ProjectMembers.SingleAsync(m => m.ProjectId == pId && m.UserId == uId);

            var removeResult = await svc.RemoveAsync(member.ProjectId, member.UserId, member.RowVersion);
            removeResult.Should().Be(DomainMutation.Updated);

            var removed = await db.ProjectMembers.SingleAsync(m => m.ProjectId == pId && m.UserId == uId);
            member.RemovedAt.Should().NotBe(null);

            var restoreResult = await svc.RestoreAsync(member.ProjectId, member.UserId, removed.RowVersion);
            restoreResult.Should().Be(DomainMutation.Updated);
            member.RemovedAt.Should().Be(null);
        }
    }
}
