using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Data;

namespace TestHelpers
{
    public static class TestDataFactory
    {
        public static byte[] Bytes(int n, byte fill = 0x5A) => Enumerable.Repeat(fill, n).ToArray();

        public static string GetRandomString(int length)
        {
            var random = new Random();
            var chars = Enumerable.Range(0, length)
                .Select(_ => (char)random.Next('a', 'z' + 1))
                .ToArray();

            return new string(chars);
        }

        // --- Helpers ---
        public static User SeedUser(AppDbContext db, string? email = null, string? name = null, UserRole role = UserRole.User)
        {
            email ??= $"{Guid.NewGuid()}@x.com";
            name ??= GetRandomString(100);

            var user = User.Create(
                Email.Create(email),
                UserName.Create(name),
                Bytes(32),
                Bytes(16),
                role
            );

            db.Users.Add(user);
            db.SaveChanges();
            return user;
        }

        public static Project SeedProject(AppDbContext db, Guid ownerId, string? name = null)
        {
            name ??= GetRandomString(100);

            var project = Project.Create(
                ownerId,
                ProjectName.Create(name),
                DateTimeOffset.UtcNow
            );

            db.Projects.Add(project);
            db.SaveChanges();
            return project;
        }

        public static ProjectMember SeedProjectMember(AppDbContext db, Guid projectId, Guid userId, ProjectRole role = ProjectRole.Member)
        {
            var projectMember = ProjectMember.Create(
                projectId,
                userId,
                role,
                DateTimeOffset.UtcNow);

            db.ProjectMembers.Add(projectMember);
            db.SaveChanges();
            return projectMember;
        }

        public static Lane SeedLane(AppDbContext db, Guid projectId, string? name = null, int order = 0)
        {
            name ??= GetRandomString(100);

            var lane = Lane.Create(
                projectId,
                LaneName.Create(name),
                order);

            db.Lanes.Add(lane);
            db.SaveChanges();
            return lane;
        }

        public static Column SeedColumn(AppDbContext db, Guid projectId, Guid laneId, string? name = null, int order = 0)
        {
            name ??= GetRandomString(100);

            var column = Column.Create(
                projectId,
                laneId,
                ColumnName.Create(name),
                order);

            db.Columns.Add(column);
            db.SaveChanges();
            return column;
        }

        public static TaskItem SeedTaskItem(AppDbContext db, Guid projectId, Guid laneId, Guid columnId,
            string? title = null, string description = "Task Description", DateTimeOffset? dueDate = null, decimal sortKey = 0m)
        {
            title ??= GetRandomString(100);

            var task = TaskItem.Create(
                columnId,
                laneId,
                projectId,
                TaskTitle.Create(title),
                TaskDescription.Create(description),
                dueDate,
                sortKey);

            db.TaskItems.Add(task);
            db.SaveChanges();
            return task;
        }

        // --- Compositions ---
        public static (Guid ProjectId, Guid UserId) SeedUserWithProject(AppDbContext db, string? userEmail = null, string? userName = null, string? projectName = null)
        {
            var user = SeedUser(db, userEmail, userName);
            var project = SeedProject(db, user.Id, projectName);
            return (project.Id, user.Id);
        }

        public static (Guid ProjectId, Guid LaneId) SeedProjectWithLane(AppDbContext db, string? userName = null, string? userEmail = null,
            string? projectName = null, string? laneName = null, int order = 0)
        {
            var user = SeedUser(db, userEmail, userName);
            var project = SeedProject(db, user.Id, projectName);
            var lane = SeedLane(db, project.Id, laneName, order);
            return (project.Id, lane.Id);
        }

        public static (Guid ProjectId, Guid LaneId, Guid ColumnId) SeedLaneWithColumn(AppDbContext db,string? userName = null, string? userEmail = null,
            string? projectName = null, string? laneName = null, string? columnName = null, int laneOrder = 0, int columnOrder = 0)
        {
            var (pId, lId) = SeedProjectWithLane(db, userName, userEmail, projectName, laneName, laneOrder);
            var column = SeedColumn(db, pId, lId, columnName, columnOrder);

            return (pId, lId, column.Id);
        }
    }
}
