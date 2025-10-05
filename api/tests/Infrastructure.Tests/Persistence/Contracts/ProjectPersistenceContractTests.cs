using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.Persistence.Contracts
{

    [Collection("SqlServerContainer")]
    public sealed class ProjectPersistenceContractTests : IClassFixture<MsSqlContainerFixture>
    {
        private readonly string _baseCs;
        public ProjectPersistenceContractTests(MsSqlContainerFixture fx) => _baseCs = fx.ContainerConnectionString;

        private static byte[] Bytes(int n, byte fill = 0x5A) => Enumerable.Repeat(fill, n).ToArray();

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

            // Owner user
            var owner = User.Create(Email.Create("owner@demo.com"), UserName.Create("Owner User"), Bytes(32), Bytes(16));
            db.Users.Add(owner);
            await db.SaveChangesAsync();

            var pName = ProjectName.Create("Alpha Board");
            var p = Project.Create(owner.Id, pName, DateTimeOffset.UtcNow);
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

            var owner1 = User.Create(Email.Create("o1@demo.com"), UserName.Create("A Owner"), Bytes(32), Bytes(16));
            var owner2 = User.Create(Email.Create("o2@demo.com"), UserName.Create("Other Owner"), Bytes(32), Bytes(16));
            db.Users.AddRange(owner1, owner2);
            await db.SaveChangesAsync();

            var name = ProjectName.Create("Same Name");

            // Same owner + same name -> throws
            var p1 = Project.Create(owner1.Id, name, DateTimeOffset.UtcNow);
            var p2 = Project.Create(owner1.Id, name, DateTimeOffset.UtcNow.AddMinutes(1));
            db.Projects.Add(p1);
            await db.SaveChangesAsync();

            db.Projects.Add(p2);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

            // Different owner + same name -> throws
            var p3 = Project.Create(owner2.Id, name, DateTimeOffset.UtcNow);
            db.Projects.Add(p3);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        [Fact]
        public async Task RowVersion_Concurrency_Throws_On_Stale_Update()
        {
            var (sp, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var owner = User.Create(Email.Create("c@demo.com"), UserName.Create("User name"), Bytes(32), Bytes(16));
            db.Users.Add(owner);
            await db.SaveChangesAsync();

            var p = Project.Create(owner.Id, ProjectName.Create("Gamma Board"), DateTimeOffset.UtcNow);
            db.Projects.Add(p);
            await db.SaveChangesAsync();

            var stale = p.RowVersion.ToArray();

            // First update
            p.Name = ProjectName.Create("Gamma Board Renamed");
            await db.SaveChangesAsync();

            // Second context with stale rowversion
            using var scope2 = sp.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
            var same = await db2.Projects.SingleAsync(x => x.Id == p.Id);

            var entry = db2.Entry(same);
            entry.Property(x => x.RowVersion).OriginalValue = stale;

            same.Name = ProjectName.Create("Another Rename");

            Func<Task> act = async () => await db2.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        }

        [Fact]
        public async Task Owner_Membership_Is_Persisted_On_Create()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var owner = User.Create(Email.Create("m@demo.com"), UserName.Create("User Name"), Bytes(32), Bytes(16));
            db.Users.Add(owner);
            await db.SaveChangesAsync();

            var p = Project.Create(owner.Id, ProjectName.Create("Owner Project"), DateTimeOffset.UtcNow);
            db.Projects.Add(p);
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();
            var members = await db.ProjectMembers.Where(x => x.ProjectId == p.Id && x.RemovedAt == null).ToListAsync();
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
