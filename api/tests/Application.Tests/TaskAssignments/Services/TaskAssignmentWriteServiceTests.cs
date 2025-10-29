using Application.Common.Abstractions.Time;
using Application.TaskActivities.Services;
using Application.TaskAssignments.Services;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using MediatR;
using Moq;
using TestHelpers;
using TestHelpers.Time;

namespace Application.Tests.TaskAssignments.Services
{
    public sealed class TaskAssignmentWriteServiceTests
    {
        private readonly IDateTimeProvider _clock = TestTime.FixedClock();

        [Fact]
        public async Task CreateAsync_Persists_Assignment_As_Created()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var uow = new UnitOfWork(db);
            var assignmentRepo = new TaskAssignmentRepository(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>();

            var assignmentWriteSvc = new TaskAssignmentWriteService(
                assignmentRepo,
                uow,
                activityWriteSvc,
                mediator.Object);

            var (_, userId) = TestDataFactory.SeedUserWithProject(db);
            var (projectId, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);

            var (result, assignment) = await assignmentWriteSvc.CreateAsync(
                projectId,
                taskId,
                targetUserId: userId,
                role: TaskRole.Owner,
                executedBy: userId);

            result.Should().Be(DomainMutation.Created);
            assignment.Should().NotBeNull();

            var exists = await assignmentRepo.ExistsAsync(taskId, userId);
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ChangeRoleAsync_Updates_And_Logs_Activity()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var uow = new UnitOfWork(db);
            var assignmentRepo = new TaskAssignmentRepository(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>();

            var assignmentWriteSvc = new TaskAssignmentWriteService(
                assignmentRepo,
                uow,
                activityWriteSvc,
                mediator.Object);

            var (_, userId) = TestDataFactory.SeedUserWithProject(db);
            var (projectId, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);
            var assignment = TestDataFactory.SeedTaskAssignment(db, taskId, userId, TaskRole.CoOwner);

            var result = await assignmentWriteSvc.ChangeRoleAsync(
                projectId,
                taskId,
                targetUserId: userId,
                newRole: TaskRole.Owner,
                executedBy: userId,
                rowVersion: assignment.RowVersion);
            result.Should().Be(DomainMutation.Updated);
        }

        [Fact]
        public async Task DeleteAsync_Deletes_And_Logs_Activity()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var uow = new UnitOfWork(db);
            var assignmentRepo = new TaskAssignmentRepository(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>();

            var assignmentWriteSvc = new TaskAssignmentWriteService(
                assignmentRepo,
                uow,
                activityWriteSvc,
                mediator.Object);

            var (_, userId) = TestDataFactory.SeedUserWithProject(db);
            var (projectId, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);
            var assignment = TestDataFactory.SeedTaskAssignment(db, taskId, userId, TaskRole.Owner);

            var result = await assignmentWriteSvc.DeleteAsync(
                projectId,
                taskId,
                targetUserId: userId,
                executedBy: userId,
                rowVersion: assignment.RowVersion);
            result.Should().Be(DomainMutation.Deleted);
        }
    }
}
