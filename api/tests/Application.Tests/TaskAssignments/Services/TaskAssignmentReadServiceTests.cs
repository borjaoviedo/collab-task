using Application.TaskAssignments.Services;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using TestHelpers.Common;
using TestHelpers.Persistence;

namespace Application.Tests.TaskAssignments.Services
{
    public sealed class TaskAssignmentReadServiceTests
    {
        [Fact]
        public async Task GetAsync_Returns_Assignment()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskAssignmentRepository(db);
            var readSvc = new TaskAssignmentReadService(repo);

            var (_, userId) = TestDataFactory.SeedUserWithProject(db);
            var (_, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);
            TestDataFactory.SeedTaskAssignment(db, taskId, userId, TaskRole.Owner);

            var assignment = await readSvc.GetAsync(taskId, userId);
            assignment.Should().NotBeNull();
        }

        [Fact]
        public async Task ListByTaskAsync_Returns_Assignments_For_Task()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskAssignmentRepository(db);
            var readSvc = new TaskAssignmentReadService(repo);

            var (_, u1) = TestDataFactory.SeedUserWithProject(db);
            var u2 = TestDataFactory.SeedUser(db).Id;
            var (_, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);

            TestDataFactory.SeedTaskAssignment(db, taskId, u1, TaskRole.Owner);
            TestDataFactory.SeedTaskAssignment(db, taskId, u2, TaskRole.CoOwner);

            var list = await readSvc.ListByTaskAsync(taskId);
            list.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListByUserAsync_Returns_Assignments_For_User()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskAssignmentRepository(db);
            var readSvc = new TaskAssignmentReadService(repo);

            var (_, userId) = TestDataFactory.SeedUserWithProject(db);
            var (_, _, _, task1) = TestDataFactory.SeedColumnWithTask(db);
            var (_, _, _, task2) = TestDataFactory.SeedColumnWithTask(db);

            TestDataFactory.SeedTaskAssignment(db, task1, userId, TaskRole.Owner);
            TestDataFactory.SeedTaskAssignment(db, task2, userId, TaskRole.CoOwner);

            var list = await readSvc.ListByUserAsync(userId);
            list.Should().HaveCount(2);
        }
    }
}
