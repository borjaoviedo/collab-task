using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Tests.Containers;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Domain.ValueObjects;


namespace Infrastructure.Tests.Auditing
{
    [Collection("SqlServerContainer")]
    public sealed class AuditingSaveChangesInterceptorTests : IClassFixture<MsSqlContainerFixture>
    {
        private readonly string _baseConnectionString;

        public AuditingSaveChangesInterceptorTests(MsSqlContainerFixture fx) => _baseConnectionString = fx.ContainerConnectionString;

        private ServiceProvider BuildProvider(string dbName)
        {
            var cs = $"{_baseConnectionString};Database={dbName}";
            var sc = new ServiceCollection();
            sc.AddInfrastructure(cs);
            return sc.BuildServiceProvider();
        }

        [Fact]
        public async Task CreatedAt_And_UpdatedAt_Are_Set_On_Insert()
        {
            using var provider = BuildProvider($"ct_{Guid.NewGuid():N}");
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();

            var p = Project.Create(Guid.NewGuid(), ProjectName.Create("Audit Test"), DateTimeOffset.UtcNow);

            db.Projects.Add(p);
            await db.SaveChangesAsync();

            p.CreatedAt.Should().NotBe(default);
            p.UpdatedAt.Should().NotBe(default);
            p.UpdatedAt!.Should().BeOnOrAfter(p.CreatedAt);
        }

        [Fact]
        public async Task UpdatedAt_Is_Changed_On_Update_And_Not_Changed_When_No_Modifications()
        {
            using var provider = BuildProvider($"ct_{Guid.NewGuid():N}");
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();

            var p = Project.Create(Guid.NewGuid(), ProjectName.Create("Audit Test 2"), DateTimeOffset.UtcNow);
            db.Projects.Add(p);
            await db.SaveChangesAsync();

            var createdAt = p.CreatedAt;
            p.Name = ProjectName.Create("Audit Test 2 Updated");
            await db.SaveChangesAsync();

            p.CreatedAt.Should().Be(createdAt);
            p.UpdatedAt.Should().NotBe(null);
            var updatedAt1 = p.UpdatedAt;

            // Save without changes → UpdatedAt must remain the same
            await db.SaveChangesAsync();
            p.UpdatedAt.Should().Be(updatedAt1);
        }
    }
}
