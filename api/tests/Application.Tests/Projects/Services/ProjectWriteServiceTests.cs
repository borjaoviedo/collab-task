using Application.Projects.Services;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers;

namespace Application.Tests.Projects.Services
{
    public sealed class ProjectWriteServiceTests
    {

        [Fact]
        public async Task CreateAsync_Returns_Created_And_Id()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);
            var svc = new ProjectWriteService(repo);

            var owner = TestDataFactory.SeedUser(db);

            var (res, id) = await svc.CreateAsync(owner.Id, "Alpha Board");

            res.Should().Be(DomainMutation.Created);
            id.Should().NotBeNull();
        }

        [Fact]
        public async Task RenameAsync_Returns_Updated_And_Recomputes_Slug()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);
            var svc = new ProjectWriteService(repo);

            var user = TestDataFactory.SeedUser(db);
            var project = TestDataFactory.SeedProject(db, user.Id);

            var res = await svc.RenameAsync(project.Id, "New Name", project.RowVersion);
            res.Should().Be(DomainMutation.Updated);

            var fromDb = await db.Projects.AsNoTracking().SingleAsync();
            fromDb.Name.Value.Should().Be("New Name");
            fromDb.Slug.Value.Should().Be("new-name");
        }

        [Fact]
        public async Task DeleteAsync_Returns_Conflict_When_RowVersion_Mismatch()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);
            var svc = new ProjectWriteService(repo);

            var user = TestDataFactory.SeedUser(db);
            var project = TestDataFactory.SeedProject(db, user.Id);

            var res = await svc.DeleteAsync(project.Id, [9, 9, 9, 9]);

            res.Should().Be(DomainMutation.Conflict);
        }
    }
}
