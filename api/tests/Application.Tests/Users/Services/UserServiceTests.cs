using Application.Common.Results;
using Application.Tests.Common.Helpers;
using Application.Users.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tests.Users.Services
{
    [Collection("SqlServerContainer")]
    public sealed class UserServiceTests : IClassFixture<MsSqlContainerFixture>
    {
        private readonly string _baseCs;
        public UserServiceTests(MsSqlContainerFixture fx) => _baseCs = fx.ContainerConnectionString;

        [Fact]
        public async Task CreateAsync_Returns_Created()
        {
            var (sp, db) = ServiceTestHelpers.BuildScope($"{_baseCs};Database=ct_{Guid.NewGuid():N}");
            var sut = sp.GetRequiredService<IUserService>();

            var user = User.Create(Email.Create("svc@ex.com"), UserName.Create("Service User"),
                                   ServiceTestHelpers.Bytes(32), ServiceTestHelpers.Bytes(16), UserRole.User);

            var res = await sut.CreateAsync(user, default);

            res.Should().Be(WriteResult.Created);
            (await db.Users.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task RenameAsync_Returns_NoOp_When_Unchanged()
        {
            var (sp, db) = ServiceTestHelpers.BuildScope($"{_baseCs};Database=ct_{Guid.NewGuid():N}");
            var sut = sp.GetRequiredService<IUserService>();

            var u = User.Create(Email.Create("n@ex.com"), UserName.Create("Same"),
                                ServiceTestHelpers.Bytes(32), ServiceTestHelpers.Bytes(16), UserRole.User);
            db.Add(u);
            await db.SaveChangesAsync();
            var rv = u.RowVersion.ToArray();

            var res = await sut.RenameAsync(u.Id, "Same", rv, default);

            res.Should().Be(WriteResult.NoOp);
        }

        [Fact]
        public async Task ChangeRoleAsync_Returns_Conflict_When_RowVersion_Mismatch()
        {
            var (sp, db) = ServiceTestHelpers.BuildScope($"{_baseCs};Database=ct_{Guid.NewGuid():N}");
            var sut = sp.GetRequiredService<IUserService>();

            var u = User.Create(Email.Create("c@ex.com"), UserName.Create("User"),
                                ServiceTestHelpers.Bytes(32), ServiceTestHelpers.Bytes(16), UserRole.User);
            db.Add(u);
            await db.SaveChangesAsync();

            var res = await sut.ChangeRoleAsync(u.Id, UserRole.Admin, new byte[] { 1, 2, 3, 4 }, default);

            res.Should().Be(WriteResult.Conflict);
        }
    }
}
