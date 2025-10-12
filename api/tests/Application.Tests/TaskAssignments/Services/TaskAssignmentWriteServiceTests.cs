using Application.TaskAssignments.Services;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using TestHelpers;

namespace Application.Tests.TaskAssignments.Services
{
    public sealed class TaskAssignmentWriteServiceTests
    {
        [Fact]
        public async Task CreateAsync_Persists_Assignment_As_Created()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskAssignmentRepository(db);
            var svc = new TaskAssignmentWriteService(repo);

            var (_, userId) = TestDataFactory.SeedUserWithProject(db);
            var (_, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);

            var (m, a) = await svc.CreateAsync(taskId, userId, TaskRole.Owner);
            m.Should().Be(DomainMutation.Created);
            a.Should().NotBeNull();

            var exists = await repo.ExistsAsync(taskId, userId);
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task AssignAsync_Delegates_To_Repo_And_Enforces_SingleOwner()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskAssignmentRepository(db);
            var svc = new TaskAssignmentWriteService(repo);

            var (_, userA) = TestDataFactory.SeedUserWithProject(db);
            var userB = TestDataFactory.SeedUser(db).Id;
            var (_, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);

            (await svc.AssignAsync(taskId, userA, TaskRole.Owner)).Should().Be(DomainMutation.Created);
            (await svc.AssignAsync(taskId, userB, TaskRole.Owner)).Should().Be(DomainMutation.Conflict);
        }

        [Fact]
        public async Task ChangeRoleAsync_Delegates_To_Repo()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskAssignmentRepository(db);
            var svc = new TaskAssignmentWriteService(repo);

            var (_, userId) = TestDataFactory.SeedUserWithProject(db);
            var (_, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);
            var assignment = TestDataFactory.SeedTaskAssignment(db, taskId, userId, TaskRole.CoOwner);

            var res = await svc.ChangeRoleAsync(taskId, userId, TaskRole.Owner, assignment.RowVersion);
            res.Should().Be(DomainMutation.Updated);
        }

        [Fact]
        public async Task RemoveAsync_Delegates_To_Repo()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskAssignmentRepository(db);
            var svc = new TaskAssignmentWriteService(repo);

            var (_, userId) = TestDataFactory.SeedUserWithProject(db);
            var (_, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);
            var assignment = TestDataFactory.SeedTaskAssignment(db, taskId, userId, TaskRole.Owner);

            var res = await svc.RemoveAsync(taskId, userId, assignment.RowVersion);
            res.Should().Be(DomainMutation.Deleted);
        }
    }
}
