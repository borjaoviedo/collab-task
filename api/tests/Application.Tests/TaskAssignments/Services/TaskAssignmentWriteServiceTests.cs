using Application.Abstractions.Time;
using Application.TaskActivities.Services;
using Application.TaskAssignments.DTOs;
using Application.TaskAssignments.Services;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using TestHelpers.Api.Fakes;
using TestHelpers.Common;
using TestHelpers.Common.Time;
using TestHelpers.Persistence;

namespace Application.Tests.TaskAssignments.Services
{
    public sealed class TaskAssignmentWriteServiceTests
    {
        private static readonly IDateTimeProvider _clock = TestTime.FixedClock();

        [Fact]
        public async Task CreateAsync_Persists_Assignment_As_Created()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc, currentUser, repo) = await CreateSutAsync(dbh);

            var (projectId, _, _, taskId, userId) = TestDataFactory.SeedColumnWithTask(db);
            currentUser.UserId = userId;

            var dto = new TaskAssignmentCreateDto
            {
                UserId = userId,
                Role = TaskRole.Owner
            };

            var assignment = await writeSvc.CreateAsync(
                projectId,
                taskId,
                dto);

            assignment.Should().NotBeNull();
            assignment.TaskId.Should().Be(taskId);
            assignment.UserId.Should().Be(userId);
            assignment.Role.Should().Be(TaskRole.Owner);

            var exists = await repo.ExistsAsync(taskId, userId);
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ChangeRoleAsync_Updates_And_Logs_Activity()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc, currentUser, _) = await CreateSutAsync(dbh);

            var (projectId, _, _, taskId, userId) = TestDataFactory.SeedColumnWithTask(db);
            currentUser.UserId = userId;

            TestDataFactory.SeedTaskAssignment(db, taskId, userId, TaskRole.CoOwner);

            var dto = new TaskAssignmentChangeRoleDto
            {
                NewRole = TaskRole.Owner
            };

            var updated = await writeSvc.ChangeRoleAsync(
                projectId,
                taskId,
                targetUserId: userId,
                dto);

            updated.Role.Should().Be(TaskRole.Owner);

            var fromDb = await db.TaskAssignments.AsNoTracking().SingleAsync();
            fromDb.Role.Should().Be(TaskRole.Owner);
        }

        [Fact]
        public async Task DeleteAsync_Deletes_And_Logs_Activity()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc, currentUser, repo) = await CreateSutAsync(dbh);

            var (projectId, _, _, taskId, userId) = TestDataFactory.SeedColumnWithTask(db);
            currentUser.UserId = userId;

            TestDataFactory.SeedTaskAssignment(db, taskId, userId, TaskRole.Owner);

            await writeSvc.DeleteAsync(
                projectId,
                taskId,
                targetUserId: userId);

            var exists = await repo.ExistsAsync(taskId, userId);
            exists.Should().BeFalse();
        }

        // ---------- HELPERS ----------

        private static Task<(CollabTaskDbContext Db, TaskAssignmentWriteService Service, FakeCurrentUserService CurrentUser, TaskAssignmentRepository Repo)>
            CreateSutAsync(
                SqliteTestDb dbh,
                Guid? userId = null)
        {
            var db = dbh.CreateContext();
            var repo = new TaskAssignmentRepository(db);
            var uow = new UnitOfWork(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var currentUser = new FakeCurrentUserService
            {
                UserId = userId
            };
            var mediator = new Mock<IMediator>();

            var svc = new TaskAssignmentWriteService(
                repo,
                uow,
                activityWriteSvc,
                currentUser,
                mediator.Object);

            return Task.FromResult((db, svc, currentUser, repo));
        }
    }
}
