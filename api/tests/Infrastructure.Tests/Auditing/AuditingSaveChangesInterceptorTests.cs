using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Tests.Containers;
using TestHelpers.Common;
using TestHelpers.Common.Testing;
using TestHelpers.Persistence;

namespace Infrastructure.Tests.Auditing
{
    [IntegrationTest]
    [SqlServerContainerTest]
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

            var user = User.Create(
                Email.Create($"{Guid.NewGuid()}@demo.com"),
                UserName.Create("Project user"),
                TestDataFactory.CreateHash(),
                TestDataFactory.CreateSalt());
            var project = Project.Create(user.Id, ProjectName.Create("Audit Test"));

            db.AddRange(user, project);
            await db.SaveChangesAsync();

            project.CreatedAt.Should().NotBe(default);
            project.UpdatedAt.Should().NotBe(default);
            project.UpdatedAt.Should().BeOnOrAfter(project.CreatedAt);
        }

        [Fact]
        public async Task UpdatedAt_Is_Changed_On_Update_And_Not_Changed_When_No_Modifications()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var user = User.Create(
                Email.Create($"{Guid.NewGuid()}@demo.com"),
                UserName.Create("Project user"),
                TestDataFactory.CreateHash(),
                TestDataFactory.CreateSalt());
            var project = Project.Create(user.Id, ProjectName.Create("Audit Test 2"));

            db.AddRange(user, project);
            await db.SaveChangesAsync();

            var createdAt = project.CreatedAt;
            project.Rename(ProjectName.Create("Audit Test 2 Updated"));
            await db.SaveChangesAsync();

            project.CreatedAt.Should().Be(createdAt);
            project.UpdatedAt.Should().NotBe(null);
            var updatedAt1 = project.UpdatedAt;

            // Save without changes -> UpdatedAt must remain the same
            await db.SaveChangesAsync();
            project.UpdatedAt.Should().Be(updatedAt1);
        }
    }
}
