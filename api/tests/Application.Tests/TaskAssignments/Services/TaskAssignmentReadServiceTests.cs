using Application.TaskAssignments.Services;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using TestHelpers.Common;
using TestHelpers.Common.Fakes;
using TestHelpers.Common.Testing;
using TestHelpers.Persistence;

namespace Application.Tests.TaskAssignments.Services
{
    [IntegrationTest]
    public sealed class TaskAssignmentReadServiceTests
    {
        [Fact]
        public async Task GetByTaskAndUserIdAsync_Returns_Assignment()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc, _) = await CreateSutAsync(dbh);

            var (_, _, _, taskId, userId) = TestDataFactory.SeedColumnWithTask(db);
            TestDataFactory.SeedTaskAssignment(db, taskId, userId, TaskRole.Owner);

            var assignment = await readSvc.GetByTaskAndUserIdAsync(taskId, userId);
            assignment.Should().NotBeNull();
        }

        [Fact]
        public async Task ListByTaskIdAsync_Returns_Assignments_For_Task()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc, _) = await CreateSutAsync(dbh);

            var (_, u1) = TestDataFactory.SeedUserWithProject(db);
            var (_, _, _, taskId, u2) = TestDataFactory.SeedColumnWithTask(db);

            TestDataFactory.SeedTaskAssignment(db, taskId, u1, TaskRole.Owner);
            TestDataFactory.SeedTaskAssignment(db, taskId, u2, TaskRole.CoOwner);

            var list = await readSvc.ListByTaskIdAsync(taskId);
            list.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListByTaskIdAsync_Returns_Assignments_For_User()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc, currentUser) = await CreateSutAsync(dbh);

            var (_, _, _, task1, userId) = TestDataFactory.SeedColumnWithTask(db);
            var (_, _, _, task2, _) = TestDataFactory.SeedColumnWithTask(db);

            TestDataFactory.SeedTaskAssignment(db, task1, userId, TaskRole.Owner);
            TestDataFactory.SeedTaskAssignment(db, task2, userId, TaskRole.CoOwner);

            // Ahora currentUser refleja al usuario en pruebas
            currentUser.UserId = userId;

            var list = await readSvc.ListByUserIdAsync(userId);
            list.Should().HaveCount(2);
        }


        // ---------- HELPERS ----------

        private static Task<(CollabTaskDbContext Db, TaskAssignmentReadService Service, FakeCurrentUserService CurrentUser)>
            CreateSutAsync(
            SqliteTestDb dbh,
            Guid? userId = null)
        {
            var db = dbh.CreateContext();
            var repo = new TaskAssignmentRepository(db);
            var currentUser = new FakeCurrentUserService
            {
                UserId = userId
            };

            var svc = new TaskAssignmentReadService(
                repo,
                currentUser);

            return Task.FromResult((db, svc, currentUser));
        }
    }
}
