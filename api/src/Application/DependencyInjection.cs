using Application.Columns.Abstractions;
using Application.Columns.Services;
using Application.Common.Validation;
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
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection.Metadata;

namespace Application
{
    /// <summary>
    /// Application layer dependency registration.
    /// </summary>
    /// <remarks>
    /// Registers application services and MediatR handlers for the Application assembly.
    /// This method is intended to be called once at startup from the composition root
    /// (e.g., Api layer). It is idempotent by design when invoked with the same
    /// <see cref="IServiceCollection"/>.
    /// </remarks>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds the Application layer services and MediatR handlers to the DI container.
        /// </summary>
        /// <param name="services">The service collection to add registrations to.</param>
        /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Register use-case services with per-request lifetime
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

            // Register all MediatR handlers/behaviors from the Application assembly
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(AssemblyReference).Assembly));

            // Registers all FluentValidation validators from the Application layer
            services.AddValidatorsFromAssembly(typeof(ApplicationValidationMarker).Assembly);

            return services;
        }
    }
}
