using Application.Abstractions.Auth;
using Application.Common.Exceptions;
using Application.ProjectMembers.Abstractions;
using Application.ProjectMembers.DTOs;
using Application.ProjectMembers.Mapping;

namespace Application.ProjectMembers.Services
{
    /// <summary>
    /// Application read-side service for <see cref="Domain.Entities.ProjectMember"/> aggregates.
    /// Provides query operations for retrieving individual membership records, listing all
    /// members of a project, resolving a user’s role within a project, and counting active
    /// memberships. All returned results are mapped to <see cref="ProjectMemberReadDto"/>
    /// representations. Missing memberships are surfaced as
    /// <see cref="NotFoundException"/> to ensure consistent error semantics
    /// at the application boundary.
    /// </summary>
    /// <param name="projectMemberRepository">
    /// Repository used for querying <see cref="Domain.Entities.ProjectMember"/> entities,
    /// including lookups by project/user pair, listing by project, role resolution,
    /// and membership analytics.
    /// </param>
    /// <param name="currentUserService">
    /// Provides information about the currently authenticated user, such as <c>UserId</c>.
    /// </param>
    public sealed class ProjectMemberReadService(
        IProjectMemberRepository projectMemberRepository,
        ICurrentUserService currentUserService) : IProjectMemberReadService
    {
        private readonly IProjectMemberRepository _projectMemberRepository = projectMemberRepository;
        private readonly ICurrentUserService _currentUserService = currentUserService;

        /// <inheritdoc/>
        public async Task<ProjectMemberReadDto> GetByProjectAndUserIdAsync(
            Guid projectId,
            Guid userId,
            CancellationToken ct = default)
        {
            var projectMember = await _projectMemberRepository.GetByProjectAndUserIdAsync(projectId, userId, ct)
                // 404 if the member does not exist
                ?? throw new NotFoundException("Member not found.");

            return projectMember.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ProjectMemberReadDto>> ListByProjectIdAsync(
            Guid projectId,
            bool includeRemoved = false,
            CancellationToken ct = default)
        {
            var projectMembers = await _projectMemberRepository.ListByProjectIdAsync(projectId, includeRemoved, ct);

            return projectMembers
                .Select(pm => pm.ToReadDto())
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<ProjectMemberRoleReadDto> GetUserRoleAsync(
            Guid projectId,
            Guid userId,
            CancellationToken ct = default)
        {
            var projectRole = await _projectMemberRepository.GetUserRoleAsync(projectId, userId, ct)
                // 404 if the member does not exist
                ?? throw new NotFoundException("Member not found.");

            return projectRole.ToRoleReadDto();
        }

        /// <inheritdoc/>
        public async Task<ProjectMemberCountReadDto> CountActiveUsersAsync(Guid userId, CancellationToken ct = default)
        {
            var count = await _projectMemberRepository.CountUserActiveMembershipsAsync(userId, ct);
            return count.ToCountReadDto();
        }

        /// <inheritdoc/>
        public async Task<ProjectMemberCountReadDto> CountActiveSelfAsync(CancellationToken ct = default)
        {
            var currentUserId = (Guid)_currentUserService.UserId!;
            var count = await _projectMemberRepository.CountUserActiveMembershipsAsync(currentUserId, ct);

            return count.ToCountReadDto();
        }
    }
}
