using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Tests.Containers;
using Infrastructure.Tests.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.Projects
{
    [Collection(nameof(DbCollection))]
    public class ProjectPersistenceTests
    {
        private readonly MsSqlContainerFixture _fx;
        public ProjectPersistenceTests(MsSqlContainerFixture fx) => _fx = fx;

        [Fact]
        public async Task Create_Project_And_GetBySlug()
        {
            using var scope = _fx.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var p = Project.Create(Guid.NewGuid(), ProjectName.Create("Alpha Board"), DateTimeOffset.UtcNow);
            db.Projects.Add(p);
            await db.SaveChangesAsync();

            var bySlug = await db.Projects.SingleAsync(x => x.Slug == ProjectSlug.Create("Alpha Board"));
            bySlug.Id.Should().Be(p.Id);
        }

        [Fact]
        public async Task ProjectMembers_Unique_Per_Project_User_And_Checks()
        {
            using var scope = _fx.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var utcNow = DateTimeOffset.UtcNow;
            var u = User.Create(Email.Create("m@demo.com"), UserName.Create("Project user"), [32], [16]);
            var p = Project.Create(u.Id, ProjectName.Create("Beta"), utcNow);
            db.AddRange(u, p);
            await db.SaveChangesAsync();

            await using var scope2 = _fx.Services.CreateAsyncScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();

            // duplicate composite key
            var pmDup = new ProjectMember(p.Id, u.Id, ProjectRole.Member, utcNow);
            db2.ProjectMembers.Add(pmDup);
            await Assert.ThrowsAsync<DbUpdateException>(() => db2.SaveChangesAsync());

            // RemovedAt before JoinedAt -> check constraint
            var pmBad = new ProjectMember(p.Id, Guid.NewGuid(), ProjectRole.Reader, utcNow)
            {
                RemovedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
            };
            db2.ProjectMembers.Add(pmBad);
            await Assert.ThrowsAsync<DbUpdateException>(() => db2.SaveChangesAsync());
        }
    }
}
