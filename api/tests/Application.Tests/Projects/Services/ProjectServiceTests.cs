using Application.Common.Results;
using Application.Projects.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Tests.Containers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Application.Tests.Common.Helpers;

namespace Application.Tests.Projects.Services
{
    [Collection("SqlServerContainer")]
    public sealed class ProjectServiceTests : IClassFixture<MsSqlContainerFixture>
    {
        private readonly string _baseCs;
        public ProjectServiceTests(MsSqlContainerFixture fx) => _baseCs = fx.ContainerConnectionString;

        private static User NewOwner() =>
            User.Create(Email.Create("owner@ex.com"), UserName.Create("Owner"),
                ServiceTestHelpers.Bytes(32), ServiceTestHelpers.Bytes(16), UserRole.User);

        [Fact]
        public async Task CreateAsync_Returns_Created()
        {
            var (sp, db) = ServiceTestHelpers.BuildScope($"{_baseCs};Database=ct_{Guid.NewGuid():N}");
            var sut = sp.GetRequiredService<IProjectService>();
            var owner = NewOwner();
            db.Add(owner);
            await db.SaveChangesAsync();

            var res = await sut.CreateAsync(owner.Id, "Alpha Board", DateTimeOffset.UtcNow, default);

            res.Should().Be(WriteResult.Created);
            (await db.Projects.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task RenameAsync_Returns_Updated_And_Recomputes_Slug()
        {
            var (sp, db) = ServiceTestHelpers.BuildScope($"{_baseCs};Database=ct_{Guid.NewGuid():N}");
            var sut = sp.GetRequiredService<IProjectService>();
            var owner = NewOwner();
            db.Add(owner);
            await db.SaveChangesAsync();

            // create
            (await sut.CreateAsync(owner.Id, "Old Name", DateTimeOffset.UtcNow, default)).Should().Be(WriteResult.Created);
            var p = await db.Projects.SingleAsync();
            var rv = p.RowVersion.ToArray();

            // rename
            var res = await sut.RenameAsync(p.Id, "New Name", rv, default);
            res.Should().Be(WriteResult.Updated);

            var fromDb = await db.Projects.AsNoTracking().SingleAsync();
            fromDb.Name.Value.Should().Be("New Name");
            fromDb.Slug.Value.Should().Be("new-name");
        }

        [Fact]
        public async Task DeleteAsync_Returns_Conflict_When_RowVersion_Mismatch()
        {
            var (sp, db) = ServiceTestHelpers.BuildScope($"{_baseCs};Database=ct_{Guid.NewGuid():N}");
            var sut = sp.GetRequiredService<IProjectService>();
            var owner = NewOwner();
            db.Add(owner);
            await db.SaveChangesAsync();

            (await sut.CreateAsync(owner.Id, "To Delete", DateTimeOffset.UtcNow, default)).Should().Be(WriteResult.Created);
            var p = await db.Projects.SingleAsync();

            var res = await sut.DeleteAsync(p.Id, new byte[] { 9, 9, 9, 9 }, default);

            res.Should().Be(WriteResult.Conflict);
        }
    }
}
