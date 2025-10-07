using Domain.Common.Exceptions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.Persistence
{
    public sealed class ProjectMemberPersistenceTests : IClassFixture<MsSqlContainerFixture>
    {
        private readonly string _baseCs;
        public ProjectMemberPersistenceTests(MsSqlContainerFixture fx) => _baseCs = fx.ContainerConnectionString;

        private (ServiceProvider sp, AppDbContext db) BuildDb(string name)
        {
            var cs = $"{_baseCs};Database={name}";
            var sc = new ServiceCollection();
            sc.AddInfrastructure(cs);
            var sp = sc.BuildServiceProvider();
            var db = sp.GetRequiredService<AppDbContext>();
            return (sp, db);
        }

        private static byte[] Bytes(int n) => Enumerable.Range(0, n).Select(_ => (byte)Random.Shared.Next(0, 255)).ToArray();

        private static (User owner, User member, Project project) NewGraph(string projectName, string ownerName, string memberName, DateTimeOffset now)
        {
            var owner = User.Create(Email.Create($"{Guid.NewGuid():N}@demo.com"), UserName.Create(ownerName), Bytes(32), Bytes(16));
            var member = User.Create(Email.Create($"{Guid.NewGuid():N}@demo.com"), UserName.Create(memberName), Bytes(32), Bytes(16));
            var project = Project.Create(owner.Id, ProjectName.Create(projectName), now);
            return (owner, member, project);
        }

        [Fact]
        public async Task Add_And_Get_By_ProjectId_And_UserId_Works()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            var (owner, member, p) = NewGraph("Alpha", "Owner", "Member", now);
            p.AddMember(member.Id, ProjectRole.Member, now);

            db.AddRange(owner, member, p);
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();
            var pm = await db.ProjectMembers.AsNoTracking().SingleAsync(x => x.ProjectId == p.Id && x.UserId == member.Id);
            pm.Role.Should().Be(ProjectRole.Member);
            pm.JoinedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void AddMember_Throws_DuplicateEntityException_When_Member_Already_Exists()
        {
            var now = DateTimeOffset.UtcNow;
            var (_, member, p) = NewGraph("Alpha", "Owner", "DupMember", now);

            p.AddMember(member.Id, ProjectRole.Member, now);

            Action act = () => p.AddMember(member.Id, ProjectRole.Member, now.AddMinutes(1));

            act.Should().Throw<DuplicateEntityException>();
        }

        [Fact]
        public async Task RowVersion_Changes_On_UpdateRole_And_On_Remove()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            var (owner, member, p) = NewGraph("Alpha", "Owner", "Member", now);
            p.AddMember(member.Id, ProjectRole.Member, now);

            db.AddRange(owner, member, p);
            await db.SaveChangesAsync();

            var m = await db.ProjectMembers.SingleAsync(x => x.ProjectId == p.Id && x.UserId == member.Id);
            var v1 = m.RowVersion.ToArray();

            // change role
            m.ChangeRole(ProjectRole.Admin);
            db.Entry(m).Property(x => x.Role).IsModified = true;
            await db.SaveChangesAsync();

            var afterRole = await db.ProjectMembers.AsNoTracking().SingleAsync(x => x.ProjectId == p.Id && x.UserId == member.Id);
            afterRole.RowVersion.Should().NotEqual(v1);

            var v2 = afterRole.RowVersion.ToArray();

            // remove
            var tracked = await db.ProjectMembers.SingleAsync(x => x.ProjectId == p.Id && x.UserId == member.Id);
            tracked.Remove(now.AddMinutes(5));
            db.Entry(tracked).Property(x => x.RemovedAt).IsModified = true;
            await db.SaveChangesAsync();

            var afterRemove = await db.ProjectMembers.AsNoTracking().SingleAsync(x => x.ProjectId == p.Id && x.UserId == member.Id);
            afterRemove.RowVersion.Should().NotEqual(v2);
            afterRemove.RemovedAt.Should().NotBeNull();
        }
    }
}
