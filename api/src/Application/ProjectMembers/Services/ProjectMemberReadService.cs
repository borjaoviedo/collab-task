using Application.Common.Exceptions;
using Application.ProjectMembers.Abstractions;
using Application.ProjectMembers.DTOs;
using Application.ProjectMembers.Mapping;
using Domain.Enums;

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
    public sealed class ProjectMemberReadService(
        IProjectMemberRepository projectMemberRepository) : IProjectMemberReadService
    {
        private readonly IProjectMemberRepository _projectMemberRepository = projectMemberRepository;

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
        public async Task<ProjectRole?> GetUserRoleAsync(
            Guid projectId,
            Guid userId,
            CancellationToken ct = default)
        {
            var projectRole = await _projectMemberRepository.GetUserRoleAsync(projectId, userId, ct)
                // 404 if the member does not exist
                ?? throw new NotFoundException("Member not found.");

            return projectRole;
        }

        /// <inheritdoc/>
        public async Task<int> CountActiveUsersAsync(Guid userId, CancellationToken ct = default)
            => await _projectMemberRepository.CountUserActiveMembershipsAsync(userId, ct);
    }
}
