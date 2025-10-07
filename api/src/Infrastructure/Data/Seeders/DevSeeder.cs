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
            var user1 = User.Create(Email.Create("admin@demo.com"), UserName.Create("Admin User"), adminHash, adminSalt, UserRole.Admin);

            var (userHash, userSalt) = hasher.Hash("User123!");
            var user2 = User.Create(Email.Create("user@demo.com"), UserName.Create("Normal User"), userHash, userSalt, UserRole.User);

            context.Users.AddRange(user1, user2);

            var utcNow = DateTimeOffset.UtcNow;

            // Demo project
            var project = Project.Create(user1.Id, ProjectName.Create("Demo Project"), utcNow);
            project.AddMember(user2.Id, ProjectRole.Member, utcNow);

            context.Projects.Add(project);

            await context.SaveChangesAsync(ct);
        }
    }
}
