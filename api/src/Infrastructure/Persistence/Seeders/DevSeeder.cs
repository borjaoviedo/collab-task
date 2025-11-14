using Application.Abstractions.Security;
using Application.TaskActivities.Payloads;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Development-only database seeder that populates demo users, projects, board structure,
    /// tasks, notes, assignments, and activity log entries. Idempotent: exits if any user exists.
    /// </summary>
    public static class DevSeeder
    {
        /// <summary>
        /// Seeds initial demo data into an empty database using a scoped service provider.
        /// Creates admin and regular users, two sample projects with lanes and columns, several tasks
        /// with ownership, notes, and corresponding <see cref="TaskActivity"/> records.
        /// </summary>
        /// <param name="services">Root service provider used to resolve scoped dependencies.</param>
        /// <param name="ct">Cancellation token.</param>
        public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (await db.Users.AnyAsync(ct)) return;

            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            DateTimeOffset Now() => DateTimeOffset.UtcNow;

            // ==== Users =====
            var (adminHash, adminSalt) = hasher.Hash("Admin123!");
            var admin = User.Create(
                Email.Create("admin@demo.com"),
                UserName.Create("Admin User"),
                adminHash,
                adminSalt,
                UserRole.Admin);

            var (userHash, userSalt) = hasher.Hash("User123!");
            var user = User.Create(
                Email.Create("user@demo.com"),
                UserName.Create("Normal User"),
                userHash,
                userSalt,
                UserRole.User);

            var (guestHash, guestSalt) = hasher.Hash("Guest123!");
            var guest = User.Create(
                Email.Create("guest@demo.com"),
                UserName.Create("Guest User"),
                guestHash,
                guestSalt,
                UserRole.User);

            db.Users.AddRange(admin, user, guest);

            // ==== Helpers =====
            decimal sort = 0m;
            decimal NextKey() => sort += 1000m;
            static ActivityPayload Payload(object o) => ActivityPayload.Create(JsonSerializer.Serialize(o));

            // ==== Project A =====
            var prjA = Project.Create(admin.Id, ProjectName.Create("Demo Project A"));
            prjA.AddMember(user.Id, ProjectRole.Admin);
            prjA.AddMember(guest.Id, ProjectRole.Member);

            var lanePlan = Lane.Create(prjA.Id, LaneName.Create("Planning"), 0);
            var laneExec = Lane.Create(prjA.Id, LaneName.Create("Execution"), 1);

            var colIdeas = Column.Create(prjA.Id, lanePlan.Id, ColumnName.Create("Ideas"), 0);
            var colTodo = Column.Create(prjA.Id, laneExec.Id, ColumnName.Create("Todo"), 0);
            var colDoing = Column.Create(prjA.Id, laneExec.Id, ColumnName.Create("Doing"), 1);
            var colReview = Column.Create(prjA.Id, laneExec.Id, ColumnName.Create("Review"), 2);
            var colDone = Column.Create(prjA.Id, laneExec.Id, ColumnName.Create("Done"), 3);

            db.Projects.Add(prjA);
            db.Lanes.AddRange(lanePlan, laneExec);
            db.Columns.AddRange(colIdeas, colTodo, colDoing, colReview, colDone);

            // -- Task A1: Ideas + note ---
            var tA1 = TaskItem.Create(
                columnId: colIdeas.Id,
                laneId: lanePlan.Id,
                projectId: prjA.Id,
                title: TaskTitle.Create("Research competitors"),
                description: TaskDescription.Create("Collect 5 differentiators."),
                dueDate: Now().AddDays(14),
                sortKey: NextKey());

            var tA1n1 = TaskNote.Create(tA1.Id, admin.Id, NoteContent.Create("Add market size estimates"));
            var tA1a1 = TaskActivity.Create(
                taskId: tA1.Id,
                userId: admin.Id,
                type: TaskActivityType.NoteAdded,
                payload: Payload(new { noteId = (Guid?)null, by = admin.Id, text = (string)NoteContent.Create("Add market size estimates") }),
                createdAt: Now()
            );

            // -- Task A2: To do + owner + coowner + note --
            var tA2 = TaskItem.Create(
                columnId: colTodo.Id,
                laneId: laneExec.Id,
                projectId: prjA.Id,
                title: TaskTitle.Create("Set up CI pipeline"),
                description: TaskDescription.Create("GitHub Actions with coverage >= 60%."),
                dueDate: Now().AddDays(7),
                sortKey: NextKey());

            var a2Own = TaskAssignment.AssignOwner(tA2.Id, user.Id);
            var a2Co = TaskAssignment.AssignCoOwner(tA2.Id, guest.Id);
            var tA2n1 = TaskNote.Create(tA2.Id, user.Id, NoteContent.Create("Start with unit tests matrix"));

            var tA2a1 = TaskActivity.Create(
                tA2.Id,
                admin.Id,
                TaskActivityType.AssignmentCreated,
                ActivityPayloadFactory.AssignmentCreated(user.Id, TaskRole.Owner),
                createdAt: Now());

            var tA2a2 = TaskActivity.Create(
                tA2.Id,
                admin.Id,
                TaskActivityType.AssignmentCreated,
                ActivityPayloadFactory.AssignmentCreated(guest.Id, TaskRole.CoOwner),
                createdAt: Now());

            var tA2a3 = TaskActivity.Create(
                tA2.Id,
                user.Id,
                TaskActivityType.NoteAdded,
                Payload(new { by = user.Id, text = (string)NoteContent.Create("Start with unit tests matrix") }),
                createdAt: Now()
            );

            // -- Task A3: Flow To do -> Doing -> Review -> Done --
            var tA3 = TaskItem.Create(
                columnId: colTodo.Id,
                laneId: laneExec.Id,
                projectId: prjA.Id,
                title: TaskTitle.Create("Implement task CRUD"),
                description: TaskDescription.Create("Endpoints + validation + repo tests"),
                dueDate: Now().AddDays(10),
                sortKey: NextKey());

            var a3Own = TaskAssignment.AssignOwner(tA3.Id, admin.Id);
            var tA3a1 = TaskActivity.Create(
                tA3.Id,
                admin.Id,
                TaskActivityType.AssignmentCreated,
                ActivityPayloadFactory.AssignmentCreated(admin.Id, TaskRole.Owner),
                createdAt: Now());

            tA3.Move(prjA.Id, laneExec.Id, colDoing.Id, NextKey());
            var tA3a2 = TaskActivity.Create(
                tA3.Id,
                admin.Id,
                TaskActivityType.TaskMoved,
                Payload(new { from = "Todo", to = "Doing" }),
                createdAt: Now());

            tA3.Edit(TaskTitle.Create("Implement task CRUD + move"), TaskDescription.Create("Add /move and rules"), tA3.DueDate);
            var tA3a3 = TaskActivity.Create(
                tA3.Id,
                admin.Id,
                TaskActivityType.TaskEdited,
                Payload(new { title = (string)TaskTitle.Create("Implement task CRUD + move") }),
                createdAt: Now());

            tA3.Move(prjA.Id, laneExec.Id, colReview.Id, NextKey());
            var tA3a4 = TaskActivity.Create(
                tA3.Id,
                admin.Id,
                TaskActivityType.TaskMoved,
                Payload(new { from = "Doing", to = "Review" }),
                createdAt: Now());

            tA3.Move(prjA.Id, laneExec.Id, colDone.Id, NextKey());
            var tA3a5 = TaskActivity.Create(
                tA3.Id,
                admin.Id,
                TaskActivityType.TaskMoved,
                Payload(new { from = "Review", to = "Done" }),
                createdAt: Now());

            // -- Task A4: Doing + edit --
            var tA4 = TaskItem.Create(
                columnId: colDoing.Id,
                laneId: laneExec.Id,
                projectId: prjA.Id,
                title: TaskTitle.Create("Board UI polish"),
                description: TaskDescription.Create("Card layout and keyboard hints"),
                dueDate: Now().AddDays(5),
                sortKey: NextKey());

            var a4Own = TaskAssignment.AssignOwner(tA4.Id, user.Id);
            var tA4a1 = TaskActivity.Create(
                tA4.Id,
                admin.Id,
                TaskActivityType.AssignmentCreated,
                ActivityPayloadFactory.AssignmentCreated(user.Id, TaskRole.Owner),
                createdAt: Now());

            tA4.Edit(TaskTitle.Create("Board UI & accessibility"), TaskDescription.Create("aria-live and keyboard DnD hints"), tA4.DueDate);
            var tA4a2 = TaskActivity.Create(
                tA4.Id,
                user.Id,
                TaskActivityType.TaskEdited,
                Payload(new { title = (string)TaskTitle.Create("Board UI & accessibility") }),
                createdAt: Now());

            db.TaskItems.AddRange(tA1, tA2, tA3, tA4);
            db.TaskNotes.AddRange(tA1n1, tA2n1);
            db.TaskAssignments.AddRange(a2Own, a2Co, a3Own, a4Own);
            db.TaskActivities.AddRange(tA1a1, tA2a1, tA2a2, tA2a3, tA3a1, tA3a2, tA3a3, tA3a4, tA3a5, tA4a1, tA4a2);

            // ==== Project B =====
            var prjB = Project.Create(user.Id, ProjectName.Create("Demo Project B"));
            prjB.AddMember(admin.Id, ProjectRole.Admin);
            prjB.AddMember(guest.Id, ProjectRole.Reader);

            var laneOps = Lane.Create(prjB.Id, LaneName.Create("Operations"), 0);
            var cBack = Column.Create(prjB.Id, laneOps.Id, ColumnName.Create("Backlog"), 0);
            var cProg = Column.Create(prjB.Id, laneOps.Id, ColumnName.Create("In Progress"), 1);
            var cDone = Column.Create(prjB.Id, laneOps.Id, ColumnName.Create("Done"), 2);

            db.Projects.Add(prjB);
            db.Lanes.Add(laneOps);
            db.Columns.AddRange(cBack, cProg, cDone);

            var tB1 = TaskItem.Create(
                columnId: cBack.Id,
                laneId: laneOps.Id,
                projectId: prjB.Id,
                title: TaskTitle.Create("Write README"),
                description: TaskDescription.Create("Setup, envs, scripts"),
                dueDate: Now().AddDays(3),
                sortKey: 1000m);

            var tB2 = TaskItem.Create(
                columnId: cProg.Id,
                laneId: laneOps.Id,
                projectId: prjB.Id,
                title: TaskTitle.Create("Add health endpoint"),
                description: TaskDescription.Create("Return version and db status"),
                dueDate: Now().AddDays(2),
                sortKey: 1000m);

            var b2Own = TaskAssignment.AssignOwner(tB2.Id, user.Id);
            var b2Act = TaskActivity.Create(
                tB2.Id,
                admin.Id,
                TaskActivityType.AssignmentCreated,
                ActivityPayloadFactory.AssignmentCreated(user.Id, TaskRole.Owner),
                createdAt: Now());

            db.TaskItems.AddRange(tB1, tB2);
            db.TaskAssignments.Add(b2Own);
            db.TaskActivities.Add(b2Act);

            // ==== Persist =====
            await db.SaveChangesAsync(ct);
        }
    }
}
