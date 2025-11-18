using Application.Abstractions.Auth;
using Application.Common.Exceptions;
using Application.Projects.Abstractions;
using Application.Projects.DTOs;
using Application.Projects.Filters;
using Application.Projects.Mapping;

namespace Application.Projects.Services
{
    /// <summary>
    /// Application read-side service for <see cref="Domain.Entities.Project"/> aggregates.
    /// Provides query operations for retrieving projects by identifier or by user,
    /// applying visibility filters when requested. All returned results are mapped
    /// to <see cref="ProjectReadDto"/> and enriched with information relevant to
    /// the currently authenticated user.
    /// </summary>
    /// <param name="projectRepository">
    /// Repository used for querying <see cref="Domain.Entities.Project"/> entities, including
    /// lookups by identifier and user associations.
    /// </param>
    /// <param name="currentUserService">
    /// Provides information about the currently authenticated user, such as
    /// <c>UserId</c>, enabling user-contextual mapping of read models.
    /// </param>
    public sealed class ProjectReadService(
        IProjectRepository projectRepository,
        ICurrentUserService currentUserService) : IProjectReadService
    {
        private readonly IProjectRepository _projectRepository = projectRepository;
        private readonly ICurrentUserService _currentUserService = currentUserService;

        /// <inheritdoc/>
        public async Task<ProjectReadDto> GetByIdAsync(Guid projectId, CancellationToken ct = default)
        {
            var project = await _projectRepository.GetByIdAsync(projectId, ct)
                // 404 if the project does not exist
                ?? throw new NotFoundException("Project not found.");

            var currentUserId = (Guid)_currentUserService.UserId!;

            return project.ToReadDto(currentUserId);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ProjectReadDto>> ListByUserIdAsync(
            Guid userId,
            ProjectFilter? filter = null,
            CancellationToken ct = default)
        {
            var projects = await _projectRepository.ListByUserIdAsync(userId, filter, ct);

            var currentUserId = (Guid)_currentUserService.UserId!;

            return projects
                .Select(p => p.ToReadDto(currentUserId))
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ProjectReadDto>> ListSelfAsync(
            ProjectFilter? filter = null,
            CancellationToken ct = default)
        {
            var currentUserId = (Guid)_currentUserService.UserId!;
            var projects = await _projectRepository.ListByUserIdAsync(currentUserId, filter, ct);

            return projects
                .Select(p => p.ToReadDto(currentUserId))
                .ToList();
        }
    }
}
