using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestHelpers;

namespace Infrastructure.Tests.Persistence.Contracts
{

    [Collection("SqlServerContainer")]
    public sealed class ProjectPersistenceContractTests : IClassFixture<MsSqlContainerFixture>
    {
        private readonly string _baseCs;
        public ProjectPersistenceContractTests(MsSqlContainerFixture fx) => _baseCs = fx.ContainerConnectionString;

        private (ServiceProvider sp, AppDbContext db) BuildDb(string name)
        {
            var cs = $"{_baseCs};Database={name}";
            var sc = new ServiceCollection();
            sc.AddInfrastructure(cs);
            var sp = sc.BuildServiceProvider();
            var db = sp.GetRequiredService<AppDbContext>();
            return (sp, db);
        }

        [Fact]
        public async Task Add_And_GetBySlug_Works()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var owner = User.Create(Email.Create("owner@demo.com"), UserName.Create("Owner User"), TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            db.Users.Add(owner);
            await db.SaveChangesAsync();

            var p = Project.Create(owner.Id, ProjectName.Create("Alpha Board"), DateTimeOffset.UtcNow);
            db.Projects.Add(p);
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();

            var found = await db.Projects.SingleOrDefaultAsync(x => x.Slug == ProjectSlug.Create("Alpha Board"));

            found.Should().NotBeNull();
            found!.Id.Should().Be(p.Id);
            found.OwnerId.Should().Be(owner.Id);
        }

        [Fact]
        public async Task Unique_Index_On_OwnerId_And_Name()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var owner1 = User.Create(Email.Create("o1@demo.com"), UserName.Create("A Owner"),
                TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            var owner2 = User.Create(Email.Create("o2@demo.com"), UserName.Create("Other Owner"),
                TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            db.Users.AddRange(owner1, owner2);
            await db.SaveChangesAsync();

            var pname = ProjectName.Create("Same Name");

            var p1 = Project.Create(owner1.Id, pname, DateTimeOffset.UtcNow);
            db.Projects.Add(p1);
            await db.SaveChangesAsync();

            // Same owner + same name -> should throw due to unique (OwnerId, Slug)
            var p2 = Project.Create(owner1.Id, pname, DateTimeOffset.UtcNow.AddMinutes(1));
            db.Projects.Add(p2);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

            // Detach p2 (or Clear/Use new scope) so it doesn't get retried
            db.Entry(p2).State = EntityState.Detached;

            // Different owner + same name -> allowed
            var p3 = Project.Create(owner2.Id, pname, DateTimeOffset.UtcNow);
            db.Projects.Add(p3);
            await db.SaveChangesAsync();

            // sanity check
            var saved = await db.Projects.AsNoTracking().SingleAsync(x => x.Id == p3.Id);
            saved.OwnerId.Should().Be(owner2.Id);
            saved.Slug.Value.Should().Be(ProjectSlug.Create("Same Name").Value);
        }

        [Fact]
        public async Task Unique_Index_On_OwnerId_And_Slug_Is_Enforced()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var owner1 = User.Create(Email.Create("o1@d.com"), UserName.Create("First owner"), TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            var owner2 = User.Create(Email.Create("o2@d.com"), UserName.Create("Second owner"), TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            db.Users.AddRange(owner1, owner2);
            await db.SaveChangesAsync();

            var p1 = Project.Create(owner1.Id, ProjectName.Create("P1"), DateTimeOffset.UtcNow);
            p1.Slug = ProjectSlug.Create("same");
            db.Projects.Add(p1);
            await db.SaveChangesAsync();

            var p2 = Project.Create(owner1.Id, ProjectName.Create("P2"), DateTimeOffset.UtcNow);
            p2.Slug = ProjectSlug.Create("same");
            db.Projects.Add(p2);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.Entry(p2).State = EntityState.Detached;

            var p3 = Project.Create(owner2.Id, ProjectName.Create("P3"), DateTimeOffset.UtcNow);
            p3.Slug = ProjectSlug.Create("same");
            db.Projects.Add(p3);
            await db.SaveChangesAsync();
        }

        [Fact]
        public async Task RowVersion_Concurrency_Throws_On_Stale_Update()
        {
            var (sp, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var owner = User.Create(Email.Create("c@demo.com"), UserName.Create("User name"), TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            db.Users.Add(owner);
            await db.SaveChangesAsync();

            var p = Project.Create(owner.Id, ProjectName.Create("Gamma Board"), DateTimeOffset.UtcNow);
            db.Projects.Add(p);
            await db.SaveChangesAsync();

            var stale = p.RowVersion.ToArray();

            p.Name = ProjectName.Create("Gamma Board Renamed");
            await db.SaveChangesAsync();

            using var scope2 = sp.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
            var same = await db2.Projects.SingleAsync(x => x.Id == p.Id);

            db2.Entry(same).Property(x => x.RowVersion).OriginalValue = stale;
            same.Name = ProjectName.Create("Another Rename");

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
        }

        [Fact]
        public async Task Owner_Membership_Is_Persisted_On_Create()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var owner = User.Create(Email.Create("m@demo.com"), UserName.Create("User Name"), TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            db.Users.Add(owner);
            await db.SaveChangesAsync();

            var p = Project.Create(owner.Id, ProjectName.Create("Owner Project"), DateTimeOffset.UtcNow);
            db.Projects.Add(p);
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();
            var members = await db.ProjectMembers
                .Where(x => x.ProjectId == p.Id && x.RemovedAt == null)
                .ToListAsync();

            members.Should().ContainSingle(m => m.UserId == owner.Id && m.Role == ProjectRole.Owner);
        }

        [Fact]
        public async Task Check_Constraints_For_Slug_And_Dates_Exist()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var checks = await db.Database
                .SqlQueryRaw<string>(
                    @"SELECT name FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.Projects')")
                .ToListAsync();

            checks.Should().Contain("CK_Projects_UpdatedAt_GTE_CreatedAt");
            checks.Should().Contain("CK_Projects_Slug_Lowercase");
            checks.Should().Contain("CK_Projects_Slug_NoSpaces");
            checks.Should().Contain("CK_Projects_Slug_NoDoubleDash");
            checks.Should().Contain("CK_Projects_Slug_NoLeadingDash");
            checks.Should().Contain("CK_Projects_Slug_NoTrailingDash");
        }
    }
}
