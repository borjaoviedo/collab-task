using Application.Common.Results;
using Application.ProjectMembers.Abstractions;
using Application.Tests.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Tests.Containers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;


namespace Application.Tests.ProjectMembers.Services
{
    [Collection("SqlServerContainer")]
    public sealed class ProjectMemberServiceTests : IClassFixture<MsSqlContainerFixture>
    {
        private readonly string _baseCs;
        public ProjectMemberServiceTests(MsSqlContainerFixture fx) => _baseCs = fx.ContainerConnectionString;

        private static User NewUser(string email, string name) =>
            User.Create(Email.Create(email), UserName.Create(name),
                ServiceTestHelpers.Bytes(32), ServiceTestHelpers.Bytes(16), UserRole.User);

        [Fact]
        public async Task AddAsync_Returns_Created_When_Not_Existing()
        {
            var (sp, db) = ServiceTestHelpers.BuildScope($"{_baseCs};Database=ct_{Guid.NewGuid():N}");
            var svc = sp.GetRequiredService<IProjectMemberService>();

            var owner = NewUser("owner@ex.com", "Owner");
            var member = NewUser("member@ex.com", "Member");
            var project = Project.Create(owner.Id, ProjectName.Create("Alpha"), DateTimeOffset.UtcNow);

            db.AddRange(owner, member, project);
            await db.SaveChangesAsync();

            var res = await svc.AddAsync(project.Id, member.Id, ProjectRole.Member, DateTimeOffset.UtcNow, default);
            res.Should().Be(WriteResult.Created);

            var countForUser = await db.ProjectMembers.CountAsync(pm => pm.ProjectId == project.Id && pm.UserId == member.Id);
            countForUser.Should().Be(1);
        }

        [Fact]
        public async Task ChangeRoleAsync_Returns_Conflict_When_RowVersion_Mismatch()
        {
            var (sp, db) = ServiceTestHelpers.BuildScope($"{_baseCs};Database=ct_{Guid.NewGuid():N}");
            var svc = sp.GetRequiredService<IProjectMemberService>();

            var owner = NewUser("o@ex.com", "Owner User");
            var member = NewUser("m@ex.com", "Member User");
            var p = Project.Create(owner.Id, ProjectName.Create("Project Name"), DateTimeOffset.UtcNow);
            db.AddRange(owner, member, p);
            await db.SaveChangesAsync();

            p.AddMember(member.Id, ProjectRole.Member, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();

            var res = await svc.ChangeRoleAsync(p.Id, member.Id, ProjectRole.Admin, new byte[] { 1, 2, 3, 4 }, default);

            res.Should().Be(WriteResult.Conflict);

            (await svc.AddAsync(p.Id, member.Id, ProjectRole.Member, DateTimeOffset.UtcNow, default)).Should().Be(WriteResult.NoOp);
        }

        [Fact]
        public async Task Remove_Then_Restore_Workflow()
        {
            var (sp, db) = ServiceTestHelpers.BuildScope($"{_baseCs};Database=ct_{Guid.NewGuid():N}");
            var svc = sp.GetRequiredService<IProjectMemberService>();

            var owner = NewUser("o2@ex.com", "Owner User");
            var member = NewUser("m2@ex.com", "Member User");
            var p = Project.Create(owner.Id, ProjectName.Create("P2"), DateTimeOffset.UtcNow);
            db.AddRange(owner, member, p);
            await db.SaveChangesAsync();

            p.AddMember(member.Id, ProjectRole.Member, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();

            var pm = await db.ProjectMembers.SingleAsync(x => x.ProjectId == p.Id && x.UserId == member.Id);
            var rv = pm.RowVersion.ToArray();

            var removed = await svc.RemoveAsync(p.Id, member.Id, rv, DateTimeOffset.UtcNow, default);
            removed.Should().Be(WriteResult.Updated);

            var refreshed = await db.ProjectMembers
                .AsNoTracking()
                .SingleAsync(x => x.ProjectId == p.Id && x.UserId == member.Id);
            refreshed.RemovedAt.Should().NotBeNull();

            // restore
            rv = (await db.ProjectMembers.SingleAsync(x => x.ProjectId == p.Id && x.UserId == member.Id)).RowVersion.ToArray();

            var restored = await svc.RestoreAsync(p.Id, member.Id, rv, default);
            restored.Should().Be(WriteResult.Updated);

            var finalPm = await db.ProjectMembers
                .AsNoTracking()
                .SingleAsync(x => x.ProjectId == p.Id && x.UserId == member.Id);

            finalPm.RemovedAt.Should().BeNull();
        }
    }
}
