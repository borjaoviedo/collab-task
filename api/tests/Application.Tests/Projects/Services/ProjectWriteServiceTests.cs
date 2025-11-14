using Application.Projects.Services;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers.Common;
using TestHelpers.Persistence;

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
            var uow = new UnitOfWork(db);
            var writeSvc = new ProjectWriteService(repo, uow);

            var owner = TestDataFactory.SeedUser(db);

            var (result, projectId) = await writeSvc.CreateAsync(owner.Id, ProjectName.Create("Alpha Board"));

            result.Should().Be(DomainMutation.Created);
            projectId.Should().NotBeNull();
        }

        [Fact]
        public async Task RenameAsync_Returns_Updated_And_Recomputes_Slug()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);
            var uow = new UnitOfWork(db);
            var writeSvc = new ProjectWriteService(repo, uow);

            var user = TestDataFactory.SeedUser(db);
            var project = TestDataFactory.SeedProject(db, user.Id);

            var result = await writeSvc.RenameAsync(project.Id, ProjectName.Create("New Name"), project.RowVersion);
            result.Should().Be(DomainMutation.Updated);

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
            var uow = new UnitOfWork(db);
            var writeSvc = new ProjectWriteService(repo, uow);

            var user = TestDataFactory.SeedUser(db);
            var project = TestDataFactory.SeedProject(db, user.Id);

            var result = await writeSvc.DeleteAsync(project.Id, [9, 9, 9, 9]);

            result.Should().Be(DomainMutation.Conflict);
        }
    }
}
