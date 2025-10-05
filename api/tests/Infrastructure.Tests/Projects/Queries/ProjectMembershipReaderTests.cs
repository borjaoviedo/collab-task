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

        public static byte[] Bytes(int n, byte fill = 0x5A) => Enumerable.Repeat(fill, n).ToArray();

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
        public async Task GetRoleAsync_Returns_Owner_Role_When_User_Creates_Project()
        {
            var dbName = $"ct_{Guid.NewGuid():N}";
            var (_, db) = BuildDb(dbName);
            await db.Database.MigrateAsync();

            var u = User.Create(Email.Create($"x{Guid.NewGuid():N}@demo.com"), UserName.Create("Member User"), Bytes(32), Bytes(16));
            var p = Project.Create(u.Id, ProjectName.Create("Test Project"), DateTimeOffset.UtcNow);
            db.AddRange(u, p);
            await db.SaveChangesAsync();

            // SUT
            var reader = new ProjectMembershipReader(db);

            var role = await reader.GetRoleAsync(p.Id, u.Id);

            role.Should().NotBeNull();
            role!.Value.Should().Be(ProjectRole.Owner);
        }

        [Fact]
        public async Task GetRoleAsync_Returns_Null_When_User_Not_In_Project()
        {
            var dbName = $"ct_{Guid.NewGuid():N}";
            var (_, db) = BuildDb(dbName);
            await db.Database.MigrateAsync();

            var u = User.Create(Email.Create($"x{Guid.NewGuid():N}@demo.com"), UserName.Create("Lonely User"), Bytes(32), Bytes(16));
            var p = Project.Create(u.Id, ProjectName.Create("Solo Project"), DateTimeOffset.UtcNow);
            db.AddRange(u, p);
            await db.SaveChangesAsync();

            var reader = new ProjectMembershipReader(db);

            var role = await reader.GetRoleAsync(p.Id, Guid.NewGuid());

            role.Should().BeNull();
        }
    }
}
