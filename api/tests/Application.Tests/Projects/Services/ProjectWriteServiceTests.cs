using Application.Projects.DTOs;
using Application.Projects.Services;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers.Api.Fakes;
using TestHelpers.Common;
using TestHelpers.Persistence;

namespace Application.Tests.Projects.Services
{
    public sealed class ProjectWriteServiceTests
    {
        [Fact]
        public async Task CreateAsync_Returns_Created_Project()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc, currentUser) = await CreateSutAsync(dbh);

            var user = TestDataFactory.SeedUser(db);
            currentUser.UserId = user.Id;

            var dto = new ProjectCreateDto { Name = "Board" };

            var project = await writeSvc.CreateAsync(dto);

            project.Should().NotBeNull();
            project.Id.Should().NotBe(Guid.Empty);
            project.Name.Should().Be("Board");

            var fromDb = await db.Projects.AsNoTracking().SingleAsync();
            fromDb.Id.Should().Be(project.Id);
            fromDb.Name.Value.Should().Be("Board");
        }

        [Fact]
        public async Task RenameAsync_Updates_Name_And_Recomputes_Slug()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc, currentUser) = await CreateSutAsync(dbh);

            var user = TestDataFactory.SeedUser(db);
            currentUser.UserId = user.Id;

            var project = TestDataFactory.SeedProject(db, user.Id);

            var dto = new ProjectRenameDto { NewName = "New Name" };

            var updated = await writeSvc.RenameAsync(project.Id, dto);

            updated.Name.Should().Be("New Name");
            updated.Slug.Should().Be("new-name");

            var fromDb = await db.Projects.AsNoTracking().SingleAsync();
            fromDb.Name.Value.Should().Be("New Name");
            fromDb.Slug.Value.Should().Be("new-name");
        }

        // ---------- HELPERS ----------

        private static Task<(CollabTaskDbContext Db, ProjectWriteService Service, FakeCurrentUserService CurrentUser)>
            CreateSutAsync(
                SqliteTestDb dbh,
                Guid? userId = null)
        {
            var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);
            var uow = new UnitOfWork(db);
            var currentUser = new FakeCurrentUserService
            {
                UserId = userId
            };

            var svc = new ProjectWriteService(
                repo,
                uow,
                currentUser);

            return Task.FromResult((db, svc, currentUser));
        }
    }
}
