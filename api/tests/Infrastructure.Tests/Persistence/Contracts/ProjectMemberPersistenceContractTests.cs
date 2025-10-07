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
    public sealed class ProjectMemberPersistenceContractTests : IClassFixture<MsSqlContainerFixture>
    {
        private readonly string _baseCs;
        public ProjectMemberPersistenceContractTests(MsSqlContainerFixture fx) => _baseCs = fx.ContainerConnectionString;

        private (ServiceProvider sp, AppDbContext db) BuildDb(string name)
        {
            var cs = $"{_baseCs};Database={name}";
            var sc = new ServiceCollection();
            sc.AddInfrastructure(cs);
            var sp = sc.BuildServiceProvider();
            var db = sp.GetRequiredService<AppDbContext>();
            return (sp, db);
        }

        private static byte[] Bytes(int n, byte fill = 0x5A) => Enumerable.Repeat(fill, n).ToArray();

        [Fact]
        public async Task Unique_Index_ProjectId_UserId_Is_Enforced()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            var owner = User.Create(Email.Create("o@demo.com"), UserName.Create("Owner"), Bytes(32), Bytes(16));
            var u = User.Create(Email.Create("m@demo.com"), UserName.Create("Member"), Bytes(32), Bytes(16));
            var p = Project.Create(owner.Id, ProjectName.Create("Alpha"), now);

            db.AddRange(owner, u, p);
            await db.SaveChangesAsync();

            var m1 = ProjectMember.Create(p.Id, u.Id, ProjectRole.Member, now);
            db.ProjectMembers.Add(m1);
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();

            var m2 = ProjectMember.Create(p.Id, u.Id, ProjectRole.Member, now.AddMinutes(1));
            db.ProjectMembers.Add(m2);

            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        [Fact]
        public async Task RowVersion_Concurrency_Throws_On_Stale_Update()
        {
            var (sp, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            var owner = User.Create(Email.Create("o@demo.com"), UserName.Create("Owner"), Bytes(32), Bytes(16));
            var u = User.Create(Email.Create("m@demo.com"), UserName.Create("Member"), Bytes(32), Bytes(16));
            var p = Project.Create(owner.Id, ProjectName.Create("Alpha"), now);
            p.AddMember(u.Id, ProjectRole.Member, now);

            db.AddRange(owner, u, p);
            await db.SaveChangesAsync();

            var current = await db.ProjectMembers.AsNoTracking().SingleAsync(x => x.ProjectId == p.Id && x.UserId == u.Id);
            var stale = current.RowVersion!.ToArray();

            // first update
            var tracked = await db.ProjectMembers.SingleAsync(x => x.ProjectId == p.Id && x.UserId == u.Id);
            tracked.ChangeRole(ProjectRole.Admin);
            db.Entry(tracked).Property(x => x.Role).IsModified = true;
            await db.SaveChangesAsync();

            // second context with stale RV
            using var scope2 = sp.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
            var same = await db2.ProjectMembers.SingleAsync(x => x.ProjectId == p.Id && x.UserId == u.Id);

            var entry = db2.Entry(same);
            entry.Property(x => x.RowVersion).OriginalValue = stale;
            same.ChangeRole(ProjectRole.Member);
            entry.Property(x => x.Role).IsModified = true;

            await FluentActions.Invoking(() => db2.SaveChangesAsync())
                .Should().ThrowAsync<DbUpdateConcurrencyException>();
        }
    }
}
