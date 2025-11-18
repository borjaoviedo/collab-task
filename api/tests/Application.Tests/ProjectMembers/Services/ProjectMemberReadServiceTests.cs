using Application.Common.Exceptions;
using Application.ProjectMembers.Services;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using TestHelpers.Common;
using TestHelpers.Common.Fakes;
using TestHelpers.Persistence;

namespace Application.Tests.ProjectMembers.Services
{
    public sealed class ProjectMemberReadServiceTests
    {
        [Fact]
        public async Task GetByProjectAndUserIdAsync_Returns_ProjectMember_When_Exists_Otherwise_Null()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc, _) = await CreateSutAsync(dbh);

            var (projectId, userId) = TestDataFactory.SeedUserWithProject(db);

            var existingResult = await readSvc.GetByProjectAndUserIdAsync(projectId, userId);
            existingResult.Should().NotBeNull();
            existingResult.ProjectId.Should().Be(projectId);
            existingResult.UserId.Should().Be(userId);

            var act = async () => await readSvc.GetByProjectAndUserIdAsync(projectId, userId: Guid.Empty);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task ListByProjectIdAsync_Returns_All_Users_By_Project()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc, _) = await CreateSutAsync(dbh);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);

            var list = await readSvc.ListByProjectIdAsync(projectId);
            list.Should().NotBeNull();
            list.Count.Should().Be(1);

            var newUser = TestDataFactory.SeedUser(db);
            TestDataFactory.SeedProjectMember(db, projectId, newUser.Id);

            list = await readSvc.ListByProjectIdAsync(projectId);
            list.Count.Should().Be(2);
        }

        [Fact]
        public async Task ListByProjectIdAsync_Returns_Empty_List_When_Not_Found_Project()
        {
            using var dbh = new SqliteTestDb();
            var (_, readSvc, _) = await CreateSutAsync(dbh);

            var list = await readSvc.ListByProjectIdAsync(projectId: Guid.NewGuid());
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task GetRoleAsync_Returns_Owner_Role_When_User_Creates_Project()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc, _) = await CreateSutAsync(dbh);

            var (projectId, userId) = TestDataFactory.SeedUserWithProject(db);

            var roleDto = await readSvc.GetUserRoleAsync(projectId, userId);
            roleDto.Should().NotBeNull();
            roleDto.Role.Should().Be(ProjectRole.Owner);
        }

        [Fact]
        public async Task GetRoleAsync_Returns_Null_When_User_Not_In_Project()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc, _) = await CreateSutAsync(dbh);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);

            var act = async () => await readSvc.GetUserRoleAsync(projectId, userId: Guid.NewGuid());
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CountActiveUsersAsync_Returns_Expected_Values()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc, _) = await CreateSutAsync(dbh);

            var user = TestDataFactory.SeedUser(db);
            var countDto = await readSvc.CountActiveUsersAsync(user.Id);
            countDto.Count.Should().Be(0);

            TestDataFactory.SeedProject(db, user.Id);
            countDto = await readSvc.CountActiveUsersAsync(user.Id);
            countDto.Count.Should().Be(1);

            var otherUser = TestDataFactory.SeedUser(db);
            var secondProject = TestDataFactory.SeedProject(db, otherUser.Id); // new project with different owner
            countDto = await readSvc.CountActiveUsersAsync(user.Id);
            countDto.Count.Should().Be(1);

            TestDataFactory.SeedProjectMember(db, secondProject.Id, user.Id);
            countDto = await readSvc.CountActiveUsersAsync(user.Id);
            countDto.Count.Should().Be(2);

            secondProject.RemoveMember(user.Id, removedAtUtc: DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();

            countDto = await readSvc.CountActiveUsersAsync(user.Id);
            countDto.Count.Should().Be(1);
        }

        // ---------- HELPERS ----------

        private static Task<(CollabTaskDbContext Db, ProjectMemberReadService Service, FakeCurrentUserService CurrentUser)>
            CreateSutAsync(
                SqliteTestDb dbh,
                Guid? userId = null)
        {
            var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);
            var currentUser = new FakeCurrentUserService
            {
                UserId = userId
            };

            var svc = new ProjectMemberReadService(
                repo,
                currentUser);

            return Task.FromResult((db, svc, currentUser));
        }
    }
}
