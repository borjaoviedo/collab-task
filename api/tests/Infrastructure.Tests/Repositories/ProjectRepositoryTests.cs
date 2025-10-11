using Application.Projects.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers;

namespace Infrastructure.Tests.Repositories
{
    public sealed class ProjectRepositoryTests
    {
        [Fact]
        public async Task GetByIdAsync_Returns_ProjectMember_When_Exists()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var (pId, _) = TestDataFactory.SeedUserWithProject(db);
            var project = await repo.GetByIdAsync(pId);

            project.Should().NotBeNull();
            project.Id.Should().Be(pId);
        }

        [Fact]
        public async Task GetByIdAsync_Returns_Null_When_Not_Found()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var project = await repo.GetByIdAsync(Guid.NewGuid());
            project.Should().BeNull();
        }

        [Fact]
        public async Task GetTrackedByIdAsync_Returns_ProjectMember_When_Exists()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var (pId, _) = TestDataFactory.SeedUserWithProject(db);
            var project = await repo.GetTrackedByIdAsync(pId);

            project.Should().NotBeNull();
            project.Id.Should().Be(pId);
        }

        [Fact]
        public async Task GetTrackedByIdAsync_Returns_Null_When_Not_Found()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var project = await repo.GetTrackedByIdAsync(Guid.NewGuid());
            project.Should().BeNull();
        }

        [Fact]
        public async Task GetAllByUserAsync_Returns_Only_Projects_Where_User_Is_Member()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            // P1 owned by U1
            var firstProjectName = "Alpha Board";
            var u1 = TestDataFactory.SeedUser(db);
            TestDataFactory.SeedProject(db, u1.Id, firstProjectName);
            // P2 owned by U2
            var secondProjectName = "Beta Board";
            var u2 = TestDataFactory.SeedUser(db);
            var p2 = TestDataFactory.SeedProject(db, u2.Id, secondProjectName);

            // Make U1 a member of P2 as well
            TestDataFactory.SeedProjectMember(db, p2.Id, u1.Id);

            var listUser1 = await repo.GetAllByUserAsync(u1.Id);
            listUser1.Select(p => p.Name.Value).Should().BeEquivalentTo(firstProjectName, secondProjectName);

            var listUser2 = await repo.GetAllByUserAsync(u2.Id);
            listUser2.Select(p => p.Name.Value).Should().BeEquivalentTo(secondProjectName);
        }

        [Fact]
        public async Task GetAllByUserAsync_Excludes_Removed_Memberships_By_Default()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var firstProjectName = "first";
            var (_, firstUserId) = TestDataFactory.SeedUserWithProject(db, projectName: firstProjectName);
            var secondUser = TestDataFactory.SeedUser(db);
            var secondProject = TestDataFactory.SeedProject(db, secondUser.Id);

            TestDataFactory.SeedProjectMember(db, secondProject.Id, firstUserId);
            secondProject.RemoveMember(firstUserId, DateTimeOffset.UtcNow.AddMinutes(1)); // sets RemovedAt
            await db.SaveChangesAsync();

            var list = await repo.GetAllByUserAsync(firstUserId, new ProjectFilter()); // IncludeRemoved=false

            list.Select(p => p.Name.Value).Should().BeEquivalentTo(firstProjectName);
        }

        [Fact]
        public async Task GetAllByUserAsync_Includes_Removed_Memberships_When_Requested()
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
            secondProject.RemoveMember(firstUserId, DateTimeOffset.UtcNow.AddMinutes(1));
            await db.SaveChangesAsync();

            var filter = new ProjectFilter { IncludeRemoved = true };
            var list = await repo.GetAllByUserAsync(firstUserId, filter);

            list.Select(p => p.Name.Value).Should().BeEquivalentTo(firstProjectName, secondProjectName);
        }

        [Fact]
        public async Task GetAllByUserAsync_Filters_By_NameContains()
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
            var list = await repo.GetAllByUserAsync(firstUserId, new ProjectFilter { NameContains = "Board", OrderBy = "name" });

            list.Select(p => p.Name.Value).Should().Equal("Alpha Board", "Gamma Board");
        }

        [Fact]
        public async Task GetAllByUserAsync_Filters_By_Role()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var firstProjectName = "OwnerOnly";
            var secondProjectName = "AsMember";
            var (_, firstUserId) = TestDataFactory.SeedUserWithProject(db, projectName: firstProjectName);
            var (secondProjectId, _) = TestDataFactory.SeedUserWithProject(db, projectName: secondProjectName);

            TestDataFactory.SeedProjectMember(db, secondProjectId, firstUserId, ProjectRole.Member);

            var onlyOwner = await repo.GetAllByUserAsync(firstUserId, new ProjectFilter { Role = ProjectRole.Owner, OrderBy = "name" });
            onlyOwner.Select(p => p.Name.Value).Should().Equal(firstProjectName);

            var onlyMember = await repo.GetAllByUserAsync(firstUserId, new ProjectFilter { Role = ProjectRole.Member, OrderBy = "name" });
            onlyMember.Select(p => p.Name.Value).Should().Equal(secondProjectName);
        }

        [Fact]
        public async Task GetAllByUserAsync_Paging_Works_With_Skip_Take()
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

            var page1 = await repo.GetAllByUserAsync(firstUserId, new ProjectFilter { OrderBy = "name", Skip = 0, Take = 2 });
            page1.Select(p => p.Name.Value).Should().Equal(firstProjectName, secondProjectName);

            var page2 = await repo.GetAllByUserAsync(firstUserId, new ProjectFilter { OrderBy = "name", Skip = 2, Take = 2 });
            page2.Select(p => p.Name.Value).Should().Equal(thirdProjectName);
        }

        [Fact]
        public async Task AddAsync_Persists_New_Project()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var projectName = "New Project";
            var owner = TestDataFactory.SeedUser(db);
            var project = Project.Create(owner.Id, ProjectName.Create(projectName), DateTimeOffset.UtcNow);

            await repo.AddAsync(project);
            await repo.SaveChangesAsync();

            var fromDb = await db.Projects.SingleAsync(p => p.Id == project.Id);
            fromDb.Name.Value.Should().Be(projectName);
        }

        [Fact]
        public async Task ExistsByNameAsync_Is_True_For_Same_Owner_And_Name()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var projectName = "Unique";
            var owner = TestDataFactory.SeedUser(db);
            TestDataFactory.SeedProject(db, owner.Id, projectName);

            var res = await repo.ExistsByNameAsync(owner.Id, projectName);
            res.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByNameAsync_Is_False_For_Different_Owner_Or_Different_Name()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var firstProjectName = "First name";
            var secondProjectName = "Second name";

            var (_, firstUserId) = TestDataFactory.SeedUserWithProject(db, projectName: firstProjectName);
            var (_, secondUserId) = TestDataFactory.SeedUserWithProject(db, projectName: secondProjectName);

            (await repo.ExistsByNameAsync(firstUserId, secondProjectName)).Should().BeFalse();
            (await repo.ExistsByNameAsync(secondUserId, firstProjectName)).Should().BeFalse();
        }

        [Fact]
        public async Task RenameAsync_NoOp_When_Name_Unchanged()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var projectName = "Project name";
            var user = TestDataFactory.SeedUser(db);
            var project = TestDataFactory.SeedProject(db, user.Id, projectName);

            var res = await repo.RenameAsync(project.Id, projectName, project.RowVersion);

            res.Should().Be(DomainMutation.NoOp);
        }

        [Fact]
        public async Task RenameAsync_Updated_And_Slug_Is_Recomputed()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var oldProjectName = "Old name";
            var newProjectName = "New name";
            var user = TestDataFactory.SeedUser(db);
            var project = TestDataFactory.SeedProject(db, user.Id, oldProjectName);

            var res = await repo.RenameAsync(project.Id, newProjectName, project.RowVersion);
            res.Should().Be(DomainMutation.Updated);

            var fromDb = await db.Projects.AsNoTracking().SingleAsync(p => p.Id == project.Id);
            fromDb.Name.Value.Should().Be(newProjectName);
            fromDb.Slug.Should().Be(ProjectSlug.Create(newProjectName));
        }

        [Fact]
        public async Task RenameAsync_Returns_Conflict_On_RowVersion_Mismatch()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var oldProjectName = "Old name";
            var newProjectName = "New name";
            var (pId, _) = TestDataFactory.SeedUserWithProject(db, projectName: oldProjectName);

            var res = await repo.RenameAsync(pId, newProjectName, [1, 2, 3, 4]);
            res.Should().Be(DomainMutation.Conflict);
        }

        [Fact]
        public async Task DeleteAsync_Returns_Deleted_When_Existing_Project()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var user = TestDataFactory.SeedUser(db);
            var project = TestDataFactory.SeedProject(db, user.Id);

            var res = await repo.DeleteAsync(project.Id, project.RowVersion);
            res.Should().Be(DomainMutation.Deleted);
        }

        [Fact]
        public async Task DeleteAsync_Returns_NotFound_When_Not_Existing_Project()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var res = await repo.DeleteAsync(Guid.NewGuid(), [1, 2]);
            res.Should().Be(DomainMutation.NotFound);
        }

        [Fact]
        public async Task DeleteAsync_Returns_Conflict_On_RowVersion_Mismatch()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            var (pId, _) = TestDataFactory.SeedUserWithProject(db);

            var res = await repo.DeleteAsync(pId, [1, 2, 3]);
            res.Should().Be(DomainMutation.Conflict);
        }
    }
}
