using Application.Columns.Abstractions;
using Application.Columns.Services;
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
using Microsoft.Extensions.DependencyInjection;
using System.Reflection.Metadata;

namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services
                .AddScoped<IUserWriteService, UserWriteService>()
                .AddScoped<IUserReadService, UserReadService>()
                .AddScoped<IProjectWriteService, ProjectWriteService>()
                .AddScoped<IProjectReadService, ProjectReadService>()
                .AddScoped<IProjectMemberWriteService, ProjectMemberWriteService>()
                .AddScoped<IProjectMemberReadService, ProjectMemberReadService>()
                .AddScoped<ILaneWriteService, LaneWriteService>()
                .AddScoped<ILaneReadService, LaneReadService>()
                .AddScoped<IColumnWriteService, ColumnWriteService>()
                .AddScoped<IColumnReadService, ColumnReadService>()
                .AddScoped<ITaskItemWriteService, TaskItemWriteService>()
                .AddScoped<ITaskItemReadService, TaskItemReadService>()
                .AddScoped<ITaskNoteWriteService, TaskNoteWriteService>()
                .AddScoped<ITaskNoteReadService, TaskNoteReadService>()
                .AddScoped<ITaskAssignmentWriteService, TaskAssignmentWriteService>()
                .AddScoped<ITaskAssignmentReadService, TaskAssignmentReadService>()
                .AddScoped<ITaskActivityWriteService, TaskActivityWriteService>()
                .AddScoped<ITaskActivityReadService, TaskActivityReadService>();

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyReference).Assembly));

            return services;
        }
    }
}
