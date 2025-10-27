using Api.Filters;
using Api.Tests.Fakes;
using Application.Columns.Abstractions;
using Application.Columns.Services;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Time;
using Application.Lanes.Abstractions;
using Application.Lanes.Services;
using Application.ProjectMembers.Abstractions;
using Application.ProjectMembers.Services;
using Application.Projects.Abstractions;
using Application.Projects.Services;
using Application.TaskActivities.Abstractions;
using Application.TaskActivities.Services;
using Application.TaskAssignments.Abstractions;
using Application.TaskAssignments.Services;
using Application.TaskItems.Abstractions;
using Application.TaskItems.Services;
using Application.TaskNotes.Abstractions;
using Application.TaskNotes.Services;
using Application.Users.Abstractions;
using Application.Users.Services;
using Infrastructure.Security;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Api.Tests.Testing
{
    public sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((ctx, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "Test",
                    ["Jwt:Audience"] = "Test",
                    ["Jwt:Key"] = new string('k', 32),
                    ["Jwt:ExpMinutes"] = "60"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Essentials
                services.AddHttpContextAccessor();
                services.AddProblemDetails();

                // Db out
                services.RemoveAll(typeof(DbContextOptions<>));
                services.RemoveAll(typeof(DbContext));

                // ===== Users =====
                services.RemoveAll(typeof(IUserRepository));
                services.AddSingleton<IUserRepository, FakeUserRepository>();
                services.RemoveAll(typeof(IUserReadService));
                services.AddScoped<IUserReadService>(sp => new UserReadService(sp.GetRequiredService<IUserRepository>()));
                services.RemoveAll(typeof(IUserWriteService));
                services.AddScoped<IUserWriteService>(sp => new UserWriteService(sp.GetRequiredService<IUserRepository>(), sp.GetRequiredService<IUnitOfWork>()));

                // ===== Projects =====
                services.RemoveAll(typeof(IProjectRepository));
                services.AddSingleton<IProjectRepository, FakeProjectRepository>();
                services.RemoveAll(typeof(IProjectReadService));
                services.AddScoped<IProjectReadService>(sp => new ProjectReadService(sp.GetRequiredService<IProjectRepository>()));
                services.RemoveAll(typeof(IProjectWriteService));
                services.AddScoped<IProjectWriteService>(sp
                    => new ProjectWriteService(sp.GetRequiredService<IProjectRepository>(), sp.GetRequiredService<IUnitOfWork>()));

                // ===== Project Members =====
                services.RemoveAll(typeof(IProjectMemberRepository));
                services.AddSingleton<IProjectMemberRepository, FakeProjectMemberRepository>();
                services.RemoveAll(typeof(IProjectMemberReadService));
                services.AddScoped<IProjectMemberReadService>(sp => new ProjectMemberReadService(sp.GetRequiredService<IProjectMemberRepository>()));
                services.RemoveAll(typeof(IProjectMemberWriteService));
                services.AddScoped<IProjectMemberWriteService>(sp
                    => new ProjectMemberWriteService(sp.GetRequiredService<IProjectMemberRepository>(), sp.GetRequiredService<IUnitOfWork>()));

                // ===== Lanes =====
                services.RemoveAll(typeof(ILaneRepository));
                services.AddSingleton<ILaneRepository, FakeLaneRepository>();
                services.RemoveAll(typeof(ILaneReadService));
                services.AddScoped<ILaneReadService>(sp => new LaneReadService(sp.GetRequiredService<ILaneRepository>()));
                services.RemoveAll(typeof(ILaneWriteService));
                services.AddScoped<ILaneWriteService>(sp
                    => new LaneWriteService(sp.GetRequiredService<ILaneRepository>(), sp.GetRequiredService<IUnitOfWork>()));

                // ===== Columns =====
                services.RemoveAll(typeof(IColumnRepository));
                services.AddSingleton<IColumnRepository, FakeColumnRepository>();
                services.RemoveAll(typeof(IColumnReadService));
                services.AddScoped<IColumnReadService>(sp => new ColumnReadService(sp.GetRequiredService<IColumnRepository>()));
                services.RemoveAll(typeof(IColumnWriteService));
                services.AddScoped<IColumnWriteService>(sp
                    => new ColumnWriteService(sp.GetRequiredService<IColumnRepository>(), sp.GetRequiredService<IUnitOfWork>()));

                // ===== Task Items =====
                services.RemoveAll(typeof(ITaskItemRepository));
                services.AddSingleton<ITaskItemRepository, FakeTaskItemRepository>();
                services.RemoveAll(typeof(ITaskItemReadService));
                services.AddScoped<ITaskItemReadService>(sp => new TaskItemReadService(sp.GetRequiredService<ITaskItemRepository>()));
                services.RemoveAll(typeof(ITaskItemWriteService));
                services.AddScoped<ITaskItemWriteService>(sp
                    => new TaskItemWriteService(
                        sp.GetRequiredService<ITaskItemRepository>(),
                        sp.GetRequiredService<IUnitOfWork>(),
                        sp.GetRequiredService<ITaskActivityWriteService>(),
                        sp.GetRequiredService<IMediator>()));

                // ===== Task Notes =====
                services.RemoveAll(typeof(ITaskNoteRepository));
                services.AddSingleton<ITaskNoteRepository, FakeTaskNoteRepository>();
                services.RemoveAll(typeof(ITaskNoteReadService));
                services.AddScoped<ITaskNoteReadService>(sp => new TaskNoteReadService(sp.GetRequiredService<ITaskNoteRepository>()));
                services.RemoveAll(typeof(ITaskNoteWriteService));
                services.AddScoped<ITaskNoteWriteService>(sp
                    => new TaskNoteWriteService(
                        sp.GetRequiredService<ITaskNoteRepository>(),
                        sp.GetRequiredService<IUnitOfWork>(),
                        sp.GetRequiredService<ITaskActivityWriteService>(),
                        sp.GetRequiredService<IMediator>()));

                // ===== Task Assignments =====
                services.RemoveAll(typeof(ITaskAssignmentRepository));
                services.AddSingleton<ITaskAssignmentRepository, FakeTaskAssignmentRepository>();
                services.RemoveAll(typeof(ITaskAssignmentReadService));
                services.AddScoped<ITaskAssignmentReadService>(sp => new TaskAssignmentReadService(sp.GetRequiredService<ITaskAssignmentRepository>()));
                services.RemoveAll(typeof(ITaskAssignmentWriteService));
                services.AddScoped<ITaskAssignmentWriteService>(sp
                    => new TaskAssignmentWriteService(
                        sp.GetRequiredService<ITaskAssignmentRepository>(),
                        sp.GetRequiredService<IUnitOfWork>(),
                        sp.GetRequiredService<ITaskActivityWriteService>(),
                        sp.GetRequiredService<IMediator>()));

                // ===== Task Activities =====
                services.RemoveAll(typeof(ITaskActivityRepository));
                services.AddSingleton<ITaskActivityRepository, FakeTaskActivityRepository>();
                services.RemoveAll(typeof(ITaskActivityReadService));
                services.AddScoped<ITaskActivityReadService>(sp => new TaskActivityReadService(sp.GetRequiredService<ITaskActivityRepository>()));
                services.RemoveAll(typeof(ITaskActivityWriteService));
                services.AddScoped<ITaskActivityWriteService>(sp
                    => new TaskActivityWriteService(sp.GetRequiredService<ITaskActivityRepository>(), sp.GetRequiredService<IDateTimeProvider>()));

                // ===== Endpoint filter para If-Match =====
                services.RemoveAll(typeof(IfMatchRowVersionFilter));
                services.AddScoped<IfMatchRowVersionFilter>();

                // ===== AuthN/AuthZ =====
                services.PostConfigure<JwtOptions>(o =>
                {
                    if (string.IsNullOrWhiteSpace(o.Key)) o.Key = new string('k', 32);
                    if (string.IsNullOrWhiteSpace(o.Issuer)) o.Issuer = "Test";
                    if (string.IsNullOrWhiteSpace(o.Audience)) o.Audience = "Test";
                    if (o.ExpMinutes <= 0) o.ExpMinutes = 60;
                });
            });
        }
    }
}
