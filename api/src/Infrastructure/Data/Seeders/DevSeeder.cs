using Application.Common.Abstractions.Security;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Data.Seeders
{
    public static class DevSeeder
    {
        public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (await context.Users.AnyAsync(ct))
                return;

            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            // Demo users
            var (adminHash, adminSalt) = hasher.Hash("Admin123!");
            var user1 = new User
            {
                Id = Guid.NewGuid(),
                Email = Email.Create("admin@demo.com"),
                PasswordHash = adminHash,
                PasswordSalt = adminSalt,
                Role = UserRole.Admin,
            };

            var (userHash, userSalt) = hasher.Hash("User123!");
            var user2 = new User
            {
                Id = Guid.NewGuid(),
                Email = Email.Create("user@demo.com"),
                PasswordHash = userHash,
                PasswordSalt = userSalt,
                Role = UserRole.User,
            };

            context.Users.AddRange(user1, user2);

            // Demo project
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = ProjectName.Create("Demo Project"),
                Slug = ProjectSlug.Create("demo-project"),
            };

            context.Projects.Add(project);

            // Demo memberships
            context.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = project.Id,
                UserId = user1.Id,
                Role = ProjectRole.Owner,
                JoinedAt = DateTimeOffset.UtcNow,
            });

            context.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = project.Id,
                UserId = user2.Id,
                Role = ProjectRole.Editor,
                JoinedAt = DateTimeOffset.UtcNow,
            });

            await context.SaveChangesAsync(ct);
        }
    }
}
