using Application.Abstractions.Persistence;
using Application.Abstractions.Time;
using Application.Common.Exceptions;
using Application.Lanes.Mapping;
using Application.ProjectMembers.Abstractions;
using Application.ProjectMembers.DTOs;
using Application.ProjectMembers.Mapping;
using Domain.Entities;
using Domain.Enums;

namespace Application.ProjectMembers.Services
{
    /// <summary>
    /// Application write-side service for <see cref="ProjectMember"/> aggregates.
    /// Handles creation, role changes, soft-removal, and restoration of project membership
    /// while enforcing membership invariants and optimistic concurrency rules.
    /// Successful write operations are persisted through <see cref="IUnitOfWork"/>,
    /// ensuring atomic updates and consistent conflict detection. Timestamped operations
    /// such as removals rely on <see cref="IDateTimeProvider"/> to guarantee deterministic
    /// and testable time behavior.
    /// </summary>
    /// <param name="projectMemberRepository">
    /// Repository responsible for querying, tracking, and persisting
    /// <see cref="ProjectMember"/> entities, including existence checks
    /// and member retrieval for update operations.
    /// </param>
    /// <param name="unitOfWork">
    /// Coordinates transactional persistence and validates <see cref="DomainMutation"/>
    /// outcomes to ensure that membership operations are applied consistently
    /// under optimistic concurrency.
    /// </param>
    /// <param name="dateTimeProvider">
    /// Abstraction for providing UTC timestamps used when removing project members,
    /// guaranteeing consistent and reproducible time-sensitive operations.
    /// </param>
    public sealed class ProjectMemberWriteService(
        IProjectMemberRepository projectMemberRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : IProjectMemberWriteService
    {
        private readonly IProjectMemberRepository _projectMemberRepository = projectMemberRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

        /// <inheritdoc/>
        public async Task<ProjectMemberReadDto> CreateAsync(
            Guid projectId,
            ProjectMemberCreateDto dto,
            CancellationToken ct = default)
        {
            if (await _projectMemberRepository.ExistsAsync(projectId, dto.UserId, ct))
                throw new ConflictException("A project member with the specified UserId already exists.");

            var projectMember = ProjectMember.Create(projectId, dto.UserId, dto.Role);
            await _projectMemberRepository.AddAsync(projectMember, ct);

            var mutation = await _unitOfWork.SaveAsync(MutationKind.Create, ct);
            if (mutation != DomainMutation.Created)
                throw new ConflictException("Project member could not be created due to a conflicting state.");

            return projectMember.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task<ProjectMemberReadDto> ChangeRoleAsync(
            Guid projectId,
            Guid userId,
            ProjectMemberChangeRoleDto dto,
            CancellationToken ct = default)
        {
            var projectMember = await _projectMemberRepository.GetByProjectAndUserIdAsync(projectId, userId, ct)
                ?? throw new NotFoundException("Project member not found.");

            projectMember.ChangeRole(dto.NewRole);

            var mutation = await _unitOfWork.SaveAsync(MutationKind.Update, ct);
            if (mutation != DomainMutation.Updated)
                throw new ConflictException("The project member role could not be changed due to a conflicting state.");

            return projectMember.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task<ProjectMemberReadDto> RemoveAsync(
            Guid projectId,
            Guid userId,
            CancellationToken ct = default)
        {
            var projectMember = await _projectMemberRepository.GetByProjectAndUserIdAsync(projectId, userId, ct)
                ?? throw new NotFoundException("Project member not found.");

            projectMember.Remove(_dateTimeProvider.UtcNow);

            var mutation = await _unitOfWork.SaveAsync(MutationKind.Update, ct);
            if (mutation != DomainMutation.Updated)
                throw new ConflictException("The project member could not be removed due to a conflicting state.");

            return projectMember.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task<ProjectMemberReadDto> RestoreAsync(
            Guid projectId,
            Guid userId,
            CancellationToken ct = default)
        {
            var projectMember = await _projectMemberRepository.GetByProjectAndUserIdAsync(projectId, userId, ct)
                ?? throw new NotFoundException("Project member not found.");

            projectMember.Restore();

            var mutation = await _unitOfWork.SaveAsync(MutationKind.Update, ct);
            if (mutation != DomainMutation.Updated)
                throw new ConflictException("The project member could not be restored due to a conflicting state.");

            return projectMember.ToReadDto();
        }
    }
}
