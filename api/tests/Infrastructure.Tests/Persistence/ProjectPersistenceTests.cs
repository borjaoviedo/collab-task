using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestHelpers;

namespace Infrastructure.Tests.Persistence
{

    [Collection("SqlServerContainer")]
    public sealed class ProjectPersistenceTests(MsSqlContainerFixture fx)
    {
        private readonly MsSqlContainerFixture _fx = fx;
        private readonly string _cs = fx.ConnectionString;

        private readonly static byte[] _validHash = TestDataFactory.Bytes(32);
        private readonly static byte[] _validSalt = TestDataFactory.Bytes(16);

        private readonly User _owner = User.Create(
                Email.Create("o@demo.com"),
                UserName.Create("Owner"),
                _validHash,
                _validSalt);

        [Fact]
        public async Task Add_And_GetBySlug_Works()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            db.Users.Add(_owner);
            await db.SaveChangesAsync();

            var projectName = "Alpha Board";
            var project = Project.Create(_owner.Id, ProjectName.Create(projectName));
            db.Projects.Add(project);
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();

            var found = await db.Projects.SingleOrDefaultAsync(p => p.Slug == ProjectSlug.Create(projectName));

            found.Should().NotBeNull();
            found!.Id.Should().Be(project.Id);
            found.OwnerId.Should().Be(_owner.Id);
        }

        [Fact]
        public async Task Unique_Index_On_OwnerId_And_Name()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var owner2 = User.Create(
                Email.Create("o2@demo.com"),
                UserName.Create("Other Owner"),
                _validHash,
                _validSalt);
            db.Users.AddRange(_owner, owner2);
            await db.SaveChangesAsync();

            var projectNameStr = "same name";
            var projectName = ProjectName.Create(projectNameStr);

            var project1 = Project.Create(_owner.Id, projectName);
            db.Projects.Add(project1);
            await db.SaveChangesAsync();

            // Same owner + same name -> should throw due to unique (OwnerId, Slug)
            var project2 = Project.Create(_owner.Id, projectName);
            db.Projects.Add(project2);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

            // Detach project2 (or Clear/Use new scope) so it doesn't get retried
            db.Entry(project2).State = EntityState.Detached;

            // Different owner + same name -> allowed
            var project3 = Project.Create(owner2.Id, projectName);
            db.Projects.Add(project3);
            await db.SaveChangesAsync();

            // sanity check
            var saved = await db.Projects
                .AsNoTracking()
                .SingleAsync(p => p.Id == project3.Id);
            saved.OwnerId.Should().Be(owner2.Id);
            saved.Slug.Value.Should().Be(ProjectSlug.Create(projectNameStr).Value);
        }

        [Fact]
        public async Task RowVersion_Concurrency_Throws_On_Stale_Update()
        {
            await _fx.ResetAsync();
            var (sp, db) = DbHelper.BuildDb(_cs);

            db.Users.Add(_owner);
            await db.SaveChangesAsync();

            var project = Project.Create(_owner.Id, ProjectName.Create("Gamma Board"));
            db.Projects.Add(project);
            await db.SaveChangesAsync();

            var stale = project.RowVersion.ToArray();

            project.Rename(ProjectName.Create("Gamma Board Renamed"));
            await db.SaveChangesAsync();

            using var scope2 = sp.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
            var same = await db2.Projects.SingleAsync(x => x.Id == project.Id);

            db2.Entry(same).Property(x => x.RowVersion).OriginalValue = stale;
            same.Rename(ProjectName.Create("Another Rename"));

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
        }

        [Fact]
        public async Task Owner_Membership_Is_Persisted_On_Create()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            db.Users.Add(_owner);
            await db.SaveChangesAsync();

            var project = Project.Create(_owner.Id, ProjectName.Create("Owner Project"));
            db.Projects.Add(project);
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();
            var members = await db.ProjectMembers
                .Where(x => x.ProjectId == project.Id && x.RemovedAt == null)
                .ToListAsync();

            members.Should().ContainSingle(m => m.UserId == _owner.Id && m.Role == ProjectRole.Owner);
        }

        [Fact]
        public async Task Check_Constraints_For_Slug_And_Dates_Exist()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var checks = await db.Database
                .SqlQueryRaw<string>(
                    @"SELECT name FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.Projects')")
                .ToListAsync();

            checks.Should().Contain("CK_Projects_UpdatedAt_GTE_CreatedAt");
            checks.Should().Contain("CK_Projects_Slug_Lowercase");
            checks.Should().Contain("CK_Projects_Slug_NoSpaces");
            checks.Should().Contain("CK_Projects_Slug_NoDoubleDash");
            checks.Should().Contain("CK_Projects_Slug_NoLeadingDash");
            checks.Should().Contain("CK_Projects_Slug_NoTrailingDash");
        }
    }
}
