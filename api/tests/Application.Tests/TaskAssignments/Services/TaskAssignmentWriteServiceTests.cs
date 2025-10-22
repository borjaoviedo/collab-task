using Application.TaskActivities.Services;
using Application.TaskAssignments.Services;
using Application.Tests.Common.Fixtures;
using Application.Tests.Common.Helpers;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using MediatR;
using Moq;
using TestHelpers;

namespace Application.Tests.TaskAssignments.Services
{
    public sealed class TaskAssignmentWriteServiceTests : BaseTest
    {
        [Fact]
        public async Task CreateAsync_Persists_Assignment_As_Created()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskAssignmentRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo, Clock);
            var mediator = new Mock<IMediator>();

            var svc = new TaskAssignmentWriteService(repo, actSvc, mediator.Object);

            var (_, userId) = TestDataFactory.SeedUserWithProject(db);
            var (pId, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);

            var (m, a) = await svc.CreateAsync(pId, taskId, affectedUserId: userId, role: TaskRole.Owner, performedBy: userId);

            m.Should().Be(DomainMutation.Created);
            a.Should().NotBeNull();

            var exists = await repo.ExistsAsync(taskId, userId);
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task AssignAsync_Delegates_And_Enforces_SingleOwner()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskAssignmentRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo, Clock);
            var mediator = new Mock<IMediator>();

            var svc = new TaskAssignmentWriteService(repo, actSvc, mediator.Object);

            var (_, userA) = TestDataFactory.SeedUserWithProject(db);
            var userB = TestDataFactory.SeedUser(db).Id;
            var (pId, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);

            (await svc.AssignAsync(pId, taskId, affectedUserId: userA, role: TaskRole.Owner, performedBy: userA))
                .Should().Be(DomainMutation.Created);

            (await svc.AssignAsync(pId, taskId, affectedUserId: userB, role: TaskRole.Owner, performedBy: userB))
                .Should().Be(DomainMutation.Conflict);
        }

        [Fact]
        public async Task ChangeRoleAsync_Updates_And_Logs_Activity()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskAssignmentRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo, Clock);
            var mediator = new Mock<IMediator>();

            var svc = new TaskAssignmentWriteService(repo, actSvc, mediator.Object);

            var (_, userId) = TestDataFactory.SeedUserWithProject(db);
            var (pId, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);
            var assignment = TestDataFactory.SeedTaskAssignment(db, taskId, userId, TaskRole.CoOwner);

            var res = await svc.ChangeRoleAsync(pId, taskId, affectedUserId: userId, newRole: TaskRole.Owner, performedBy: userId, rowVersion: assignment.RowVersion);
            res.Should().Be(DomainMutation.Updated);
        }

        [Fact]
        public async Task RemoveAsync_Deletes_And_Logs_Activity()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskAssignmentRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo, Clock);
            var mediator = new Mock<IMediator>();

            var svc = new TaskAssignmentWriteService(repo, actSvc, mediator.Object);

            var (_, userId) = TestDataFactory.SeedUserWithProject(db);
            var (pId, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);
            var assignment = TestDataFactory.SeedTaskAssignment(db, taskId, userId, TaskRole.Owner);

            var res = await svc.RemoveAsync(pId, taskId, affectedUserId: userId, performedBy: userId, rowVersion: assignment.RowVersion);
            res.Should().Be(DomainMutation.Deleted);
        }
    }
}
