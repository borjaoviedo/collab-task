using Application.Abstractions.Auth;
using Application.Abstractions.Persistence;
using Application.Common.Exceptions;
using Application.Projects.Abstractions;
using Application.Projects.DTOs;
using Application.Projects.Mapping;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Projects.Services
{
    /// <summary>
    /// Application write-side service for <see cref="Project"/> aggregates.
    /// Handles creation, renaming, and deletion of projects. All write operations
    /// enforce domain invariants (such as name validity), uniqueness constraints,
    /// and optimistic concurrency through <see cref="IUnitOfWork"/> using
    /// <see cref="DomainMutation"/> and <see cref="MutationKind"/>.
    /// </summary>
    /// <param name="projectRepository">
    /// Repository used for retrieving, tracking, modifying and persisting
    /// <see cref="Project"/> entities, including name-based lookups for uniqueness.
    /// </param>
    /// <param name="unitOfWork">
    /// Coordinates atomic persistence of changes and enforces optimistic concurrency.
    /// </param>
    /// <param name="currentUserService">
    /// Provides the identity of the currently authenticated user, used to associate
    /// new projects with their owner and to contextualize read models.
    /// </param>
    public sealed class ProjectWriteService(
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService) : IProjectWriteService
    {
        private readonly IProjectRepository _projectRepository = projectRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ICurrentUserService _currentUserService = currentUserService;

        /// <inheritdoc/>
        public async Task<ProjectReadDto> CreateAsync(
            ProjectCreateDto dto,
            CancellationToken ct = default)
        {
            var projectNameVo = ProjectName.Create(dto.Name);
            if (await _projectRepository.ExistsByNameAsync(projectNameVo, ct))
                throw new ConflictException("A project with the specified name already exists.");

            var currentUserId = (Guid)_currentUserService.UserId!;

            var project = Project.Create(currentUserId, projectNameVo);
            await _projectRepository.AddAsync(project, ct);

            var mutation = await _unitOfWork.SaveAsync(MutationKind.Create, ct);
            if (mutation != DomainMutation.Created)
                throw new ConflictException("Project could not be created due to a conflicting state.");


            return project.ToReadDto(currentUserId);
        }

        /// <inheritdoc/>
        public async Task<ProjectReadDto> RenameAsync(
            Guid projectId,
            ProjectRenameDto dto,
            CancellationToken ct = default)
        {
            var project = await _projectRepository.GetByIdForUpdateAsync(projectId, ct)
                ?? throw new NotFoundException("Project not found.");

            var newNameVo = ProjectName.Create(dto.NewName);
            project.Rename(newNameVo);

            var mutation = await _unitOfWork.SaveAsync(MutationKind.Update, ct);
            if (mutation != DomainMutation.Updated)
                throw new ConflictException("The project name could not be changed due to a conflicting state.");

            var currentUserId = (Guid)_currentUserService.UserId!;

            return project.ToReadDto(currentUserId);
        }

        /// <inheritdoc/>
        public async Task DeleteByIdAsync(
            Guid projectId,
            CancellationToken ct = default)
        {
            var project = await _projectRepository.GetByIdForUpdateAsync(projectId, ct)
                ?? throw new NotFoundException("Project not found.");

            await _projectRepository.RemoveAsync(project, ct);
            var mutation = await _unitOfWork.SaveAsync(MutationKind.Delete, ct);

            if (mutation != DomainMutation.Deleted && mutation != DomainMutation.NoOp)
                throw new ConflictException("The project could not be deleted due to a conflicting state.");
        }
    }
}
