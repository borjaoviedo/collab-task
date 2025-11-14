using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Persistence;
using System.Security.Cryptography;
using TestHelpers.Common.Time;

namespace TestHelpers.Common
{
    public static class TestDataFactory
    {

        // --- Primitives / Generators ---

        public static byte[] Bytes(int n, byte fill = 0x5A) => Enumerable.Repeat(fill, n).ToArray();

        private static byte[] SecureBytes(int length)
        {
            var buffer = new byte[length];
            RandomNumberGenerator.Fill(buffer);
            return buffer;
        }

        public static byte[] CreateHash() => SecureBytes(32);

        public static byte[] CreateSalt() => SecureBytes(16);

        public static string GetRandomString(int length)
        {
            var random = new Random();
            var chars = Enumerable.Range(0, length)
                .Select(_ => (char)random.Next('a', 'z' + 1))
                .ToArray();

            return new string(chars);
        }


        // --- Seed: Core Entities ---

        public static User SeedUser(
            CollabTaskDbContext db,
            string? email = null,
            string? name = null,
            UserRole role = UserRole.User)
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

        public static Project SeedProject(
            CollabTaskDbContext db,
            Guid ownerId,
            string? name = null)
        {
            name ??= GetRandomString(100);

            var project = Project.Create(
                ownerId,
                ProjectName.Create(name)
            );

            db.Projects.Add(project);
            db.SaveChanges();
            return project;
        }

        public static ProjectMember SeedProjectMember(
            CollabTaskDbContext db,
            Guid projectId,
            Guid userId,
            ProjectRole role = ProjectRole.Member)
        {
            var projectMember = ProjectMember.Create(
                projectId,
                userId,
                role);

            db.ProjectMembers.Add(projectMember);
            db.SaveChanges();
            return projectMember;
        }


        // --- Seed: Board Structure ---

        public static Lane SeedLane(
            CollabTaskDbContext db,
            Guid projectId,
            string? name = null,
            int order = 0)
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

        public static Column SeedColumn(
            CollabTaskDbContext db,
            Guid projectId,
            Guid laneId,
            string? name = null,
            int order = 0)
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

        public static TaskItem SeedTaskItem(
            CollabTaskDbContext db,
            Guid projectId,
            Guid laneId,
            Guid columnId,
            string? title = null,
            string description = "Task Description",
            DateTimeOffset? dueDate = null,
            decimal sortKey = 0m)
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


        // --- Seed: Task Details ---

        public static TaskNote SeedTaskNote(
            CollabTaskDbContext db,
            Guid taskId,
            Guid authorId,
            string? content = null)
        {
            content ??= GetRandomString(200);

            var note = TaskNote.Create(
                taskId,
                authorId,
                NoteContent.Create(content));

            db.TaskNotes.Add(note);
            db.SaveChanges();
            return note;
        }

        public static TaskAssignment SeedTaskAssignment(
            CollabTaskDbContext db,
            Guid taskId,
            Guid userId,
            TaskRole role = TaskRole.Owner)
        {
            var assignment = TaskAssignment.Create(taskId, userId, role);

            db.TaskAssignments.Add(assignment);
            db.SaveChanges();
            return assignment;
        }

        public static TaskActivity SeedTaskActivity(
            CollabTaskDbContext db,
            Guid taskId,
            Guid userId,
            TaskActivityType type = TaskActivityType.TaskCreated,
            string? payloadData = null)
        {
            payloadData ??= $"{{\"message\":\"{GetRandomString(20)}\"}}";

            var payload = ActivityPayload.Create(payloadData);
            var activity = TaskActivity.Create(
                taskId,
                userId,
                type,
                payload,
                createdAt: TestTime.FixedNow);

            db.TaskActivities.Add(activity);
            db.SaveChanges();

            return activity;
        }


        // --- Compositions ---

        public static (Guid ProjectId, Guid UserId) SeedUserWithProject(
            CollabTaskDbContext db,
            string? userEmail = null,
            string? userName = null,
            string? projectName = null)
        {
            var user = SeedUser(db, userEmail, userName);
            var project = SeedProject(db, user.Id, projectName);
            return (project.Id, user.Id);
        }

        public static (
            Guid ProjectId,
            Guid LaneId,
            Guid UserId)
            SeedProjectWithLane(
            CollabTaskDbContext db,
            string? userName = null,
            string? userEmail = null,
            string? projectName = null,
            string? laneName = null,
            int order = 0)
        {
            var (projectId, userId) = SeedUserWithProject(
                db,
                userEmail,
                userName,
                projectName);
            var lane = SeedLane(
                db,
                projectId,
                laneName,
                order);

            return (projectId, lane.Id, userId);
        }

        public static (
            Guid ProjectId,
            Guid LaneId,
            Guid ColumnId,
            Guid UserId)
            SeedLaneWithColumn(
            CollabTaskDbContext db,
            string? userName = null,
            string? userEmail = null,
            string? projectName = null,
            string? laneName = null,
            string? columnName = null,
            int laneOrder = 0,
            int columnOrder = 0)
        {
            var (projectId, laneId, userId) = SeedProjectWithLane(
                db,
                userName,
                userEmail,
                projectName,
                laneName,
                laneOrder);
            var column = SeedColumn(
                db,
                projectId,
                laneId,
                columnName,
                columnOrder);

            return (projectId, laneId, column.Id, userId);
        }

        public static (
            Guid ProjectId,
            Guid LaneId,
            Guid ColumnId,
            Guid TaskId,
            Guid UserId)
            SeedColumnWithTask(
            CollabTaskDbContext db,
            string? userName = null,
            string? userEmail = null,
            string? projectName = null,
            string? laneName = null,
            string? columnName = null,
            string? taskTitle = null,
            string taskDescription = "Task Description",
            DateTimeOffset? dueDate = null,
            decimal sortKey = 0m,
            int laneOrder = 0,
            int columnOrder = 0)
        {
            var (projectId, laneId, columnId, userId) = SeedLaneWithColumn(
                db,
                userName,
                userEmail,
                projectName,
                laneName,
                columnName,
                laneOrder,
                columnOrder);
            var task = SeedTaskItem(
                db,
                projectId,
                laneId,
                columnId,
                taskTitle,
                taskDescription,
                dueDate,
                sortKey);

            return (projectId, laneId, columnId, task.Id, userId);
        }

        public static (
            Guid ProjectId,
            Guid LaneId,
            Guid ColumnId,
            Guid TaskId,
            Guid TaskNoteId,
            Guid UserId)
            SeedFullBoard(
            CollabTaskDbContext db,
            string? userName = null,
            string? userEmail = null,
            string? projectName = null,
            string? laneName = null,
            string? columnName = null,
            string? taskTitle = null,
            string taskDescription = "Task Description",
            DateTimeOffset? dueDate = null,
            decimal sortKey = 0m,
            int laneOrder = 0,
            int columnOrder = 0,
            string? noteContent = null)
        {
            var (projectId, laneId, columnId, taskId, userId) = SeedColumnWithTask(
                db,
                userName,
                userEmail,
                projectName,
                laneName,
                columnName,
                taskTitle,
                taskDescription,
                dueDate,
                sortKey,
                laneOrder,
                columnOrder);
            var taskNote = SeedTaskNote(db, taskId, userId, noteContent);

            return (projectId, laneId, columnId, taskId, taskNote.Id, userId);
        }
    }
}
