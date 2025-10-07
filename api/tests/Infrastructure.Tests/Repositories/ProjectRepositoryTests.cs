using Application.Projects.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.Repositories
{
    public sealed class ProjectRepositoryTests : IClassFixture<MsSqlContainerFixture>
    {
        private readonly string _baseCs;

        public ProjectRepositoryTests(MsSqlContainerFixture fx) => _baseCs = fx.ContainerConnectionString;

        private (ServiceProvider sp, AppDbContext db, ProjectRepository repo) BuildDb(string name)
        {
            var cs = $"{_baseCs};Database={name}";
            var sc = new ServiceCollection();
            sc.AddInfrastructure(cs);
            var sp = sc.BuildServiceProvider();
            var db = sp.GetRequiredService<AppDbContext>();
            var repo = new ProjectRepository(db);
            return (sp, db, repo);
        }

        private static byte[] Bytes(int n) => Enumerable.Range(0, n).Select(_ => (byte)Random.Shared.Next(0, 255)).ToArray();

        private static (User owner, Project project) NewProject(string projectName, string userName, DateTimeOffset now)
        {
            var owner = User.Create(Email.Create($"{userName}@demo.com"), UserName.Create(userName), Bytes(32), Bytes(16));
            var project = Project.Create(owner.Id, ProjectName.Create(projectName), now);
            return (owner, project);
        }

        [Fact]
        public async Task GetByIdAsync_Returns_Project_When_Exists()
        {
            var (_, db, repo) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            var (owner, p) = NewProject("Alpha", "User", now);
            db.AddRange(owner, p);
            await db.SaveChangesAsync();

            var fromRepo = await repo.GetByIdAsync(p.Id);

            fromRepo.Should().NotBeNull();
            fromRepo!.Id.Should().Be(p.Id);
        }

        [Fact]
        public async Task GetByIdAsync_Returns_Null_When_Not_Found()
        {
            var (_, db, repo) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var fromRepo = await repo.GetByIdAsync(Guid.NewGuid());
            fromRepo.Should().BeNull();
        }

        [Fact]
        public async Task GetByUserAsync_Returns_Only_Projects_Where_User_Is_Member()
        {
            var (_, db, repo) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;

            // P1 owned by U1
            var (u1, p1) = NewProject("Alpha Board", "John", now);
            // P2 owned by U2
            var (u2, p2) = NewProject("Beta Board", "Doe", now);
            // Make U1 a member of P2 as well
            p2.AddMember(u1.Id, ProjectRole.Member, now);

            db.AddRange(u1, u2, p1, p2);
            await db.SaveChangesAsync();

            var listUser1 = await repo.GetByUserAsync(u1.Id);
            listUser1.Select(x => x.Name.Value).Should().BeEquivalentTo("Alpha Board", "Beta Board");

            var listUser2 = await repo.GetByUserAsync(u2.Id);
            listUser2.Select(x => x.Name.Value).Should().BeEquivalentTo("Beta Board");
        }

        [Fact]
        public async Task GetByUserAsync_Excludes_Removed_Memberships_By_Default()
        {
            var (_, db, repo) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;

            var (u1, p1) = NewProject("Visible", "John", now);
            var (u2, p2) = NewProject("Removed", "Doe", now);

            p2.AddMember(u1.Id, ProjectRole.Member, now);
            p2.RemoveMember(u1.Id, now.AddMinutes(1)); // sets RemovedAt

            db.AddRange(u1, u2, p1, p2);
            await db.SaveChangesAsync();

            var list = await repo.GetByUserAsync(u1.Id, new ProjectFilter()); // IncludeRemoved=false

            list.Select(x => x.Name.Value).Should().BeEquivalentTo("Visible");
        }

        [Fact]
        public async Task GetByUserAsync_Includes_Removed_Memberships_When_Requested()
        {
            var (_, db, repo) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;

            var (u1, p1) = NewProject("Visible", "John", now);
            var (u2, p2) = NewProject("Removed", "Doe", now);

            p2.AddMember(u1.Id, ProjectRole.Member, now);
            p2.RemoveMember(u1.Id, now.AddMinutes(1));

            db.AddRange(u1, u2, p1, p2);
            await db.SaveChangesAsync();

            var filter = new ProjectFilter { IncludeRemoved = true };
            var list = await repo.GetByUserAsync(u1.Id, filter);

            list.Select(x => x.Name.Value).Should().BeEquivalentTo("Visible", "Removed");
        }

        [Fact]
        public async Task GetByUserAsync_Filters_By_NameContains()
        {
            var (_, db, repo) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            var (u1, p1) = NewProject("Gamma Board", "John", now);
            var (u2, p2) = NewProject("Beta Desk", "Doe", now);
            var (u3, p3) = NewProject("Alpha Board", "Junior", now);

            p3.AddMember(u1.Id, ProjectRole.Member, now);

            db.AddRange(u1, u2, u3, p1, p2, p3);
            await db.SaveChangesAsync();

            var list = await repo.GetByUserAsync(u1.Id, new ProjectFilter { NameContains = "Board", OrderBy = "name" });

            list.Select(x => x.Name.Value).Should().Equal("Alpha Board", "Gamma Board");
        }

        [Fact]
        public async Task GetByUserAsync_Filters_By_Role()
        {
            var (_, db, repo) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;

            var (uOwner, pOwnerOnly) = NewProject("OwnerOnly", "John", now);
            var (uSecondOwner, pWhereMember) = NewProject("AsMember", "Doe", now);
            pWhereMember.AddMember(uOwner.Id, ProjectRole.Member, now);

            db.AddRange(uOwner, uSecondOwner, pOwnerOnly, pWhereMember);
            await db.SaveChangesAsync();

            var onlyOwner = await repo.GetByUserAsync(uOwner.Id, new ProjectFilter { Role = ProjectRole.Owner, OrderBy = "name" });
            onlyOwner.Select(x => x.Name.Value).Should().Equal("OwnerOnly");

            var onlyMember = await repo.GetByUserAsync(uOwner.Id, new ProjectFilter { Role = ProjectRole.Member, OrderBy = "name" });
            onlyMember.Select(x => x.Name.Value).Should().Equal("AsMember");
        }

        [Fact]
        public async Task GetByUserAsync_Sorts_By_Name_And_By_CreatedAt_And_By_UpdatedAt()
        {
            var (_, db, repo) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var t0 = DateTimeOffset.UtcNow;

            var (u, pA) = NewProject("PA", "John", t0.AddMinutes(-10));
            var (u2, pB) = NewProject("PB", "Doe", t0.AddMinutes(-5));
            var (u3, pC) = NewProject("PC", "Senior", t0);

            pB.AddMember(u.Id, ProjectRole.Member, t0);
            pC.AddMember(u.Id, ProjectRole.Member, t0);

            db.AddRange(u, u2, u3, pA, pB, pC);
            await db.SaveChangesAsync();

            await db.Database.ExecuteSqlRawAsync(
                @"UPDATE Projects SET CreatedAt = {0} WHERE Id = {1};
                  UPDATE Projects SET CreatedAt = {2} WHERE Id = {3};
                  UPDATE Projects SET CreatedAt = {4} WHERE Id = {5};",
                t0.AddMinutes(-10), pA.Id,
                t0.AddMinutes(-5), pB.Id,
                t0, pC.Id);

            // Simulate updates so UpdatedAt differs
            await db.Database.ExecuteSqlRawAsync(
                @"UPDATE Projects SET UpdatedAt = {0} WHERE Id = {1};
                  UPDATE Projects SET UpdatedAt = {2} WHERE Id = {3};
                  UPDATE Projects SET UpdatedAt = {4} WHERE Id = {5};",
                t0.AddMinutes(1), pB.Id,
                t0.AddMinutes(2), pA.Id,
                t0.AddMinutes(3), pC.Id);

            var byNameDesc = await repo.GetByUserAsync(u.Id, new ProjectFilter { OrderBy = "name_desc" });
            byNameDesc.Select(x => x.Name.Value).Should().Equal("PC", "PB", "PA");

            var byCreatedAsc = await repo.GetByUserAsync(u.Id, new ProjectFilter { OrderBy = "createdat" });
            byCreatedAsc.Select(x => x.Name.Value).Should().Equal("PA", "PB", "PC");

            var byUpdatedDescDefault = await repo.GetByUserAsync(u.Id, new ProjectFilter { });
            byUpdatedDescDefault[0].Name.Value.Should().Be("PC");
        }

        [Fact]
        public async Task GetByUserAsync_Paging_Works_With_Skip_Take()
        {
            var (_, db, repo) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            var (u, p1) = NewProject("P1", "John", now);
            var (u2, p2) = NewProject("P2", "Doe", now);
            var (u3, p3) = NewProject("P3", "Junior", now);

            p2.AddMember(u.Id, ProjectRole.Member, now);
            p3.AddMember(u.Id, ProjectRole.Member, now);

            db.AddRange(u, u2, u3, p1, p2, p3);
            await db.SaveChangesAsync();

            var page1 = await repo.GetByUserAsync(u.Id, new ProjectFilter { OrderBy = "name", Skip = 0, Take = 2 });
            page1.Select(x => x.Name.Value).Should().Equal("P1", "P2");

            var page2 = await repo.GetByUserAsync(u.Id, new ProjectFilter { OrderBy = "name", Skip = 2, Take = 2 });
            page2.Select(x => x.Name.Value).Should().Equal("P3");
        }

        [Fact]
        public async Task AddAsync_Persists_New_Project()
        {
            var (_, db, repo) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            var (owner, p) = NewProject("New Project", "John", now);

            db.Add(owner);
            await repo.AddAsync(p);
            await db.SaveChangesAsync();

            var fromDb = await db.Projects.SingleAsync(x => x.Id == p.Id);
            fromDb.Name.Value.Should().Be("New Project");
        }

        [Fact]
        public async Task ExistsByNameAsync_Is_True_For_Same_Owner_And_Name()
        {
            var (_, db, repo) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            var (owner, p) = NewProject("Unique Per Owner", "John", now);
            db.AddRange(owner, p);
            await db.SaveChangesAsync();

            var exists = await repo.ExistsByNameAsync(owner.Id, ProjectName.Create("Unique Per Owner"));
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByNameAsync_Is_False_For_Different_Owner_Or_Different_Name()
        {
            var (_, db, repo) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            var (owner1, p1) = NewProject("Same Name", "Same", now);
            var (owner2, _) = NewProject("Other Name", "Other", now);

            db.AddRange(owner1, owner2, p1);
            await db.SaveChangesAsync();

            (await repo.ExistsByNameAsync(owner1.Id, ProjectName.Create("Other Name"))).Should().BeFalse();
            (await repo.ExistsByNameAsync(owner2.Id, ProjectName.Create("Same Name"))).Should().BeFalse();
        }

        [Fact]
        public async Task RenameAsync_NoOp_When_Name_Unchanged()
        {
            var (_, db, repo) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            var (owner, p) = NewProject("Alpha", "User", now);
            db.AddRange(owner, p);
            await db.SaveChangesAsync();
            var rv = p.RowVersion.ToArray();

            var res = await repo.RenameAsync(p.Id, "Alpha", rv);

            res.Should().Be(DomainMutation.NoOp);
            (await db.SaveChangesAsync()).Should().Be(0);
        }

        [Fact]
        public async Task RenameAsync_Updated_And_Slug_Is_Recomputed()
        {
            var (_, db, repo) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            var (owner, p) = NewProject("Old Name", "User", now);
            db.AddRange(owner, p);
            await db.SaveChangesAsync();
            var rv = p.RowVersion.ToArray();

            var res = await repo.RenameAsync(p.Id, "New Name", rv);
            res.Should().Be(DomainMutation.Updated);

            await db.SaveChangesAsync();
            var fromDb = await db.Projects.AsNoTracking().SingleAsync(x => x.Id == p.Id);
            fromDb.Name.Value.Should().Be("New Name");
            fromDb.Slug.Value.Should().Be("new-name");
        }

        [Fact]
        public async Task RenameAsync_Updated_But_SaveChanges_Throws_On_RowVersion_Mismatch()
        {
            var (_, db, repo) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            var (owner, p) = NewProject("Alpha", "User", now);
            db.AddRange(owner, p);
            await db.SaveChangesAsync();

            var res = await repo.RenameAsync(p.Id, "Beta", new byte[] { 1, 2, 3, 4 });
            res.Should().Be(DomainMutation.Updated);

            await FluentActions.Invoking(() => db.SaveChangesAsync())
                .Should().ThrowAsync<DbUpdateConcurrencyException>();
        }

        [Fact]
        public async Task DeleteAsync_Deleted_But_SaveChanges_Throws_On_RowVersion_Mismatch()
        {
            var (_, db, repo) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            var (owner, p) = NewProject("ToDelete", "User", now);
            db.AddRange(owner, p);
            await db.SaveChangesAsync();

            var res = await repo.DeleteAsync(p.Id, new byte[] { 9, 9, 9 });
            res.Should().Be(DomainMutation.Deleted);

            await FluentActions.Invoking(() => db.SaveChangesAsync())
                .Should().ThrowAsync<DbUpdateConcurrencyException>();
        }
    }
}
