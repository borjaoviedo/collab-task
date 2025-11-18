using Application.Abstractions.Time;
using Application.ProjectMembers.DTOs;
using Application.ProjectMembers.Services;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers.Common;
using TestHelpers.Common.Testing;
using TestHelpers.Common.Time;
using TestHelpers.Persistence;

namespace Application.Tests.ProjectMembers.Services
{
    [IntegrationTest]
    public sealed class ProjectMemberWriteServiceTests
    {
        private static readonly IDateTimeProvider _clock = TestTime.FixedClock();

        [Fact]
        public async Task CreateAsync_Returns_Created_When_Not_Existing()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc) = await CreateSutAsync(dbh);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);
            var user = TestDataFactory.SeedUser(db);

            var dto = new ProjectMemberCreateDto
            {
                Role = ProjectRole.Member,
                UserId = user.Id
            };

            var projectMember = await writeSvc.CreateAsync(projectId, dto);

            projectMember.Should().NotBeNull();
            projectMember.ProjectId.Should().Be(projectId);
            projectMember.UserId.Should().Be(user.Id);
            projectMember.Role.Should().Be(ProjectRole.Member);
        }

        [Fact]
        public async Task RemoveAsync_Then_Restore_Workflow()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc) = await CreateSutAsync(dbh);

            var (projectId, userId) = TestDataFactory.SeedUserWithProject(db);
            var member = await db.ProjectMembers
                .SingleAsync(m => m.ProjectId == projectId && m.UserId == userId);

            var removeResult = await writeSvc.RemoveAsync(member.ProjectId, member.UserId);
            removeResult.Should().NotBeNull();
            removeResult.ProjectId.Should().Be(member.ProjectId);
            removeResult.UserId.Should().Be(member.UserId);

            var restoreResult = await writeSvc.RestoreAsync(member.ProjectId, member.UserId);
            restoreResult.Should().NotBeNull();
            restoreResult.ProjectId.Should().Be(member.ProjectId);
            restoreResult.UserId.Should().Be(member.UserId);

            var restored = await db.ProjectMembers
                .SingleAsync(m => m.ProjectId == projectId && m.UserId == userId);
            restored.RemovedAt.Should().BeNull();
        }

        // ---------- HELPERS ----------

        private static Task<(CollabTaskDbContext Db, ProjectMemberWriteService Service)>
            CreateSutAsync(SqliteTestDb dbh)
        {
            var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);
            var uow = new UnitOfWork(db);

            var svc = new ProjectMemberWriteService(
                repo,
                uow,
                _clock);

            return Task.FromResult((db, svc));
        }
    }
}
