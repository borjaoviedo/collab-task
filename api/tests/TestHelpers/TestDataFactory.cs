using Domain.Entities;
using Domain.ValueObjects;
using Infrastructure.Data;

namespace TestHelpers
{
    public static class TestDataFactory
    {
        public static byte[] Bytes(int n, byte fill = 0x5A) => Enumerable.Repeat(fill, n).ToArray();

        // --- Helpers ---
        public static User SeedUser(AppDbContext db, string email = "random@x.com", string name = "Random User")
        {
            var user = User.Create(
                Email.Create(email),
                UserName.Create(name),
                Bytes(32),
                Bytes(16)
            );

            db.Users.Add(user);
            db.SaveChanges();
            return user;
        }

        public static Project SeedProject(AppDbContext db, Guid ownerId, string name = "Project")
        {
            var project = Project.Create(
                ownerId,
                ProjectName.Create(name),
                DateTimeOffset.UtcNow
            );

            db.Projects.Add(project);
            db.SaveChanges();
            return project;
        }

        public static Lane SeedLane(AppDbContext db, Guid projectId, string name = "Lane", int order = 0)
        {
            var lane = Lane.Create(
                projectId,
                LaneName.Create(name),
                order);

            db.Lanes.Add(lane);
            db.SaveChanges();
            return lane;
        }

        public static Column SeedColumn(AppDbContext db, Guid projectId, Guid laneId, string name = "Column", int order = 0)
        {
            var column = Column.Create(
                projectId,
                laneId,
                ColumnName.Create(name),
                order);

            db.Columns.Add(column);
            db.SaveChanges();
            return column;
        }

        // --- Compositions ---
        public static Guid SeedUserWithProject(AppDbContext db)
        {
            var user = SeedUser(db);
            var project = SeedProject(db, user.Id);
            return project.Id;
        }

        public static (Guid ProjectId, Guid LaneId) SeedProjectWithLane(AppDbContext db, string projectName = "Project", string laneName = "Lane", int order = 0)
        {
            var user = SeedUser(db);
            var project = SeedProject(db, user.Id, projectName);
            var lane = SeedLane(db, project.Id, laneName, order);
            return (project.Id, lane.Id);
        }
    }
}
