using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Projects.Queries;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.Projects.Queries
{
    public sealed class ProjectMembershipReaderTests : IClassFixture<MsSqlContainerFixture>
    {

        private readonly string _baseCs;
        public ProjectMembershipReaderTests(MsSqlContainerFixture fx) => _baseCs = fx.ContainerConnectionString;

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
        public async Task GetRoleAsync_Returns_Member_Role_When_User_Is_Member()
        {
            var dbName = $"ct_{Guid.NewGuid():N}";
            var (_, db) = BuildDb(dbName);
            await db.Database.MigrateAsync();

            var userId = Guid.NewGuid();

            db.Users.Add(User.Create(Email.Create($"m{Guid.NewGuid():N}@demo.com"), UserName.Create("Member User"), [1], [2]));

            var utcNow = DateTimeOffset.UtcNow;
            var p = Project.Create(userId, ProjectName.Create("Test Project"), utcNow);
            db.Projects.Add(p);

            await db.SaveChangesAsync();

            // SUT
            var reader = new ProjectMembershipReader(db);

            var role = await reader.GetRoleAsync(p.Id, userId);

            role.Should().NotBeNull();
            role!.Value.Should().Be(ProjectRole.Member);
        }

        [Fact]
        public async Task GetRoleAsync_Returns_Null_When_User_Not_In_Project()
        {
            var dbName = $"ct_{Guid.NewGuid():N}";
            var (_, db) = BuildDb(dbName);
            await db.Database.MigrateAsync();

            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            db.Users.Add(User.Create(Email.Create($"x{Guid.NewGuid():N}@demo.com"), UserName.Create("Lonely User"), [1], [2]));

            db.Projects.Add(Project.Create(Guid.NewGuid(), ProjectName.Create("Solo Project"), DateTimeOffset.UtcNow));

            await db.SaveChangesAsync();

            var reader = new ProjectMembershipReader(db);

            var role = await reader.GetRoleAsync(projectId, userId);

            role.Should().BeNull();
        }
    }
}
