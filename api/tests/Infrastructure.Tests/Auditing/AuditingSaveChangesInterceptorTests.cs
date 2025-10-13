using FluentAssertions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Domain.ValueObjects;
using TestHelpers;
using Infrastructure.Tests.Containers;

namespace Infrastructure.Tests.Auditing
{
    [Collection("SqlServerContainer")]
    public sealed class AuditingSaveChangesInterceptorTests(MsSqlContainerFixture fx)
    {
        private readonly MsSqlContainerFixture _fx = fx;
        private readonly string _cs = fx.ConnectionString;

        [Fact]
        public async Task CreatedAt_And_UpdatedAt_Are_Set_On_Insert()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var u = User.Create(Email.Create($"{Guid.NewGuid()}@demo.com"), UserName.Create("Project user"), TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            var p = Project.Create(u.Id, ProjectName.Create("Audit Test"), DateTimeOffset.UtcNow);

            db.AddRange(u, p);
            await db.SaveChangesAsync();

            p.CreatedAt.Should().NotBe(default);
            p.UpdatedAt.Should().NotBe(default);
            p.UpdatedAt!.Should().BeOnOrAfter(p.CreatedAt);
        }

        [Fact]
        public async Task UpdatedAt_Is_Changed_On_Update_And_Not_Changed_When_No_Modifications()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var u = User.Create(Email.Create($"{Guid.NewGuid()}@demo.com"), UserName.Create("Project user"), TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            var p = Project.Create(u.Id, ProjectName.Create("Audit Test 2"), DateTimeOffset.UtcNow);

            db.AddRange(u, p);
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
