using Application.Projects.Filters;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers.Common;
using TestHelpers.Common.Testing;
using TestHelpers.Persistence;

namespace Infrastructure.Tests.Repositories
{
    [IntegrationTest]
    public sealed class ProjectRepositoryTests
    {
        [Fact]
        public async Task GetByIdAsync_Returns_ProjectMember_When_Exists()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);
            var project = await repo.GetByIdAsync(projectId);

            project.Should().NotBeNull();
            project.Id.Should().Be(projectId);
        }

        [Fact]
        public async Task GetByIdAsync_Returns_Null_When_Not_Found()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var project = await repo.GetByIdAsync(projectId: Guid.NewGuid());
            project.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_Returns_ProjectMember_When_Exists()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);
            var project = await repo.GetByIdForUpdateAsync(projectId);

            project.Should().NotBeNull();
            project.Id.Should().Be(projectId);
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_Returns_Null_When_Not_Found()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var project = await repo.GetByIdForUpdateAsync(projectId: Guid.NewGuid());
            project.Should().BeNull();
        }

        [Fact]
        public async Task ListByUserIdAsync_Returns_Only_Projects_Where_User_Is_Member()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            // P1 owned by U1
            var firstProjectName = "Alpha Board";
            var user1 = TestDataFactory.SeedUser(db);
            TestDataFactory.SeedProject(db, user1.Id, firstProjectName);

            // P2 owned by U2
            var secondProjectName = "Beta Board";
            var user2 = TestDataFactory.SeedUser(db);
            var project2 = TestDataFactory.SeedProject(db, user2.Id, secondProjectName);

            // Make U1 a member of P2 as well
            TestDataFactory.SeedProjectMember(db, project2.Id, user1.Id);

            var listUser1 = await repo.ListByUserIdAsync(user1.Id);
            listUser1.Select(p => p.Name.Value).Should().BeEquivalentTo(firstProjectName, secondProjectName);

            var listUser2 = await repo.ListByUserIdAsync(user2.Id);
            listUser2.Select(p => p.Name.Value).Should().BeEquivalentTo(secondProjectName);
        }

        [Fact]
        public async Task ListByUserIdAsync_Excludes_Removed_Memberships_By_Default()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var firstProjectName = "first";
            var (_, firstUserId) = TestDataFactory.SeedUserWithProject(db, projectName: firstProjectName);
            var secondUser = TestDataFactory.SeedUser(db);
            var secondProject = TestDataFactory.SeedProject(db, secondUser.Id);

            TestDataFactory.SeedProjectMember(db, secondProject.Id, firstUserId);
            secondProject.RemoveMember(firstUserId, removedAtUtc: DateTimeOffset.UtcNow.AddMinutes(1)); // sets RemovedAt
            await db.SaveChangesAsync();

            var list = await repo.ListByUserIdAsync(firstUserId, filter: new ProjectFilter()); // IncludeRemoved=false

            list.Select(p => p.Name.Value).Should().BeEquivalentTo(firstProjectName);
        }

        [Fact]
        public async Task ListByUserIdAsync_Includes_Removed_Memberships_When_Requested()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var firstProjectName = "first";
            var secondProjectName = "second";
            var (_, firstUserId) = TestDataFactory.SeedUserWithProject(db, projectName: firstProjectName);
            var secondUser = TestDataFactory.SeedUser(db);
            var secondProject = TestDataFactory.SeedProject(db, secondUser.Id, secondProjectName);

            TestDataFactory.SeedProjectMember(db, secondProject.Id, firstUserId);
            secondProject.RemoveMember(firstUserId, removedAtUtc: DateTimeOffset.UtcNow.AddMinutes(1));
            await db.SaveChangesAsync();

            var filter = new ProjectFilter { IncludeRemoved = true };
            var list = await repo.ListByUserIdAsync(firstUserId, filter);

            list.Select(p => p.Name.Value).Should().BeEquivalentTo(firstProjectName, secondProjectName);
        }

        [Fact]
        public async Task ListByUserIdAsync_Filters_By_NameContains()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var firstProjectName = "Gamma Board";
            var secondProjectName = "Beta Desk";
            var thirdProjectName = "Alpha Board";

            var(_, firstUserId) = TestDataFactory.SeedUserWithProject(db, projectName: firstProjectName);
            var (_, secondUserId) = TestDataFactory.SeedUserWithProject(db, projectName: secondProjectName);
            var thirdProject = TestDataFactory.SeedProject(db, secondUserId, thirdProjectName);

            TestDataFactory.SeedProjectMember(db, thirdProject.Id, firstUserId);
            var filter = new ProjectFilter { NameContains = "Board", OrderBy = ProjectOrderBy.NameAsc };
            var list = await repo.ListByUserIdAsync(firstUserId, filter);

            list.Select(p => p.Name.Value).Should().Equal(thirdProjectName, firstProjectName);
        }

        [Fact]
        public async Task ListByUserIdAsync_Filters_By_Role()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var firstProjectName = "OwnerOnly";
            var secondProjectName = "AsMember";
            var (_, firstUserId) = TestDataFactory.SeedUserWithProject(db, projectName: firstProjectName);
            var (secondProjectId, _) = TestDataFactory.SeedUserWithProject(db, projectName: secondProjectName);

            TestDataFactory.SeedProjectMember(db, secondProjectId, firstUserId, ProjectRole.Member);

            var firstFilter = new ProjectFilter { Role = ProjectRole.Owner, OrderBy = ProjectOrderBy.NameAsc };
            var onlyOwner = await repo.ListByUserIdAsync(firstUserId, firstFilter);
            onlyOwner.Select(p => p.Name.Value).Should().Equal(firstProjectName);

            var secondFilter = new ProjectFilter { Role = ProjectRole.Member, OrderBy = ProjectOrderBy.NameAsc };
            var onlyMember = await repo.ListByUserIdAsync(firstUserId, secondFilter);
            onlyMember.Select(p => p.Name.Value).Should().Equal(secondProjectName);
        }

        [Fact]
        public async Task ListByUserIdAsync_Paging_Works_With_Skip_Take()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var firstProjectName = "P1";
            var secondProjectName = "P2";
            var thirdProjectName = "P3";

            var (_, firstUserId) = TestDataFactory.SeedUserWithProject(db, projectName: firstProjectName);
            var (secondProjectId, _) = TestDataFactory.SeedUserWithProject(db, projectName: secondProjectName);
            var (thirdProjectId, _) = TestDataFactory.SeedUserWithProject(db, projectName: thirdProjectName);

            TestDataFactory.SeedProjectMember(db, secondProjectId, firstUserId);
            TestDataFactory.SeedProjectMember(db, thirdProjectId, firstUserId);

            var firstFilter = new ProjectFilter { OrderBy = ProjectOrderBy.NameAsc, Skip = 0, Take = 2 };
            var page1 = await repo.ListByUserIdAsync(firstUserId, firstFilter);
            page1.Select(p => p.Name.Value).Should().Equal(firstProjectName, secondProjectName);

            var secondFilter = new ProjectFilter { OrderBy = ProjectOrderBy.NameAsc, Skip = 2, Take = 2 };
            var page2 = await repo.ListByUserIdAsync(firstUserId, secondFilter);
            page2.Select(p => p.Name.Value).Should().Equal(thirdProjectName);
        }

        [Fact]
        public async Task ExistsByNameAsync_Is_True_For_Same_Owner_And_Name()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var projectName = ProjectName.Create("Unique");
            var owner = TestDataFactory.SeedUser(db);
            TestDataFactory.SeedProject(db, owner.Id, projectName);

            var result = await repo.ExistsByNameAsync(projectName);
            result.Should().BeTrue();
        }

        // --------------- Add / Update / Remove ---------------

        [Fact]
        public async Task AddAsync_Persists_New_Project()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var projectName = ProjectName.Create("New Project");
            var owner = TestDataFactory.SeedUser(db);
            var project = Project.Create(owner.Id, ProjectName.Create(projectName));

            await repo.AddAsync(project);
            await db.SaveChangesAsync();

            var fromDb = await db.Projects.SingleAsync(p => p.Id == project.Id);
            fromDb.Name.Value.Should().Be(projectName);
        }

        [Fact]
        public async Task UpdateAsync_Marks_Entity_Modified_And_Persists_Changes()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);

            var project = await repo.GetByIdForUpdateAsync(projectId);

            // Modify through domain behavior
            project!.Rename(ProjectName.Create("Updated Name"));

            await repo.UpdateAsync(project);
            await db.SaveChangesAsync();

            var reloaded = await db.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == projectId);

            reloaded.Should().NotBeNull();
            reloaded!.Name.Value.Should().Be("Updated Name");
        }

        [Fact]
        public async Task RemoveAsync_Deletes_User_On_SaveChanges()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);

            var project = await repo.GetByIdForUpdateAsync(projectId);

            await repo.RemoveAsync(project!);
            await db.SaveChangesAsync();

            var reloaded = await db.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == projectId);

            reloaded.Should().BeNull();
        }
    }
}
