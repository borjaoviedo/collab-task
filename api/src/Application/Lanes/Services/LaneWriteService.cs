using Application.Abstractions.Persistence;
using Application.Columns.Mapping;
using Application.Common.Exceptions;
using Application.Lanes.Abstractions;
using Application.Lanes.DTOs;
using Application.Lanes.Mapping;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Lanes.Services
{
    /// <summary>
    /// Application write-side service for <see cref="Lane"/> aggregates.
    /// Handles creation, renaming, deletion, and two-phase reordering of lanes within a project board,
    /// enforcing name uniqueness, ordering invariants, and optimistic concurrency guarantees.
    /// All write operations are persisted atomically through <see cref="IUnitOfWork"/>,
    /// ensuring that modifications to lane structure remain consistent even under concurrent access.
    /// </summary>
    /// <param name="laneRepository">
    /// Repository responsible for querying, tracking, and mutating <see cref="Lane"/> entities,
    /// including name uniqueness checks, reorder preparation, and reorder finalization.
    /// </param>
    /// <param name="unitOfWork">
    /// Coordinates transactional persistence and validates <see cref="DomainMutation"/> results,
    /// guaranteeing that lane operations either complete atomically or fail safely under concurrency conflicts.
    /// </param>
    public sealed class LaneWriteService(
        ILaneRepository laneRepository,
        IUnitOfWork unitOfWork) : ILaneWriteService
    {
        private readonly ILaneRepository _laneRepository = laneRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        /// <inheritdoc/>
        public async Task<LaneReadDto> CreateAsync(
            Guid projectId,
            LaneCreateDto dto,
            CancellationToken ct = default)
        {
            var laneNameVo = LaneName.Create(dto.Name);
            if (await _laneRepository.ExistsWithNameAsync(projectId, laneNameVo, ct: ct))
                throw new ConflictException("A lane with the specified name already exists.");

            var lane = Lane.Create(projectId, laneNameVo, dto.Order);
            await _laneRepository.AddAsync(lane, ct);

            var mutation = await _unitOfWork.SaveAsync(MutationKind.Create, ct);
            if (mutation != DomainMutation.Created)
                throw new ConflictException("Lane could not be created due to a conflicting state.");

            return lane.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task<LaneReadDto> RenameAsync(
            Guid laneId,
            LaneRenameDto dto,
            CancellationToken ct = default)
        {
            var lane = await _laneRepository.GetByIdForUpdateAsync(laneId, ct)
                ?? throw new NotFoundException("Lane not found.");

            var newNameVo = LaneName.Create(dto.NewName);
            lane.Rename(newNameVo);

            var mutation = await _unitOfWork.SaveAsync(MutationKind.Update, ct);
            if (mutation != DomainMutation.Updated)
                throw new ConflictException("The lane name could not be changed due to a conflicting state.");

            return lane.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task<LaneReadDto> ReorderAsync(
            Guid laneId,
            LaneReorderDto dto,
            CancellationToken ct = default)
        {
            var precheck = await _laneRepository.PrepareReorderAsync(
                laneId,
                dto.NewOrder,
                ct);

            if (precheck is PrecheckStatus.NotFound)
                throw new NotFoundException("Lane not found.");

            if (precheck is PrecheckStatus.NoOp)
            {
                var current = await _laneRepository.GetByIdAsync(laneId, ct)
                    ?? throw new NotFoundException("Lane not found.");

                return current.ToReadDto();
            }

            var firstMutation = await _unitOfWork.SaveAsync(MutationKind.Update, ct);
            if (firstMutation == DomainMutation.Conflict)
                throw new ConflictException("The lane could not be reordered due to a conflicting state.");

            await _laneRepository.FinalizeReorderAsync(laneId, ct);

            var secondMutation = await _unitOfWork.SaveAsync(MutationKind.Update, ct);
            if (secondMutation != DomainMutation.Updated)
                throw new ConflictException("The lane could not be reordered due to a conflicting state.");

            var reloaded = await _laneRepository.GetByIdAsync(laneId, ct)
                ?? throw new NotFoundException("Lane not found after reorder.");

            return reloaded.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task DeleteByIdAsync(Guid laneId, CancellationToken ct = default)
        {
            var lane = await _laneRepository.GetByIdForUpdateAsync(laneId, ct)
                ?? throw new NotFoundException("Lane not found.");

            await _laneRepository.RemoveAsync(lane, ct);
            var mutation = await _unitOfWork.SaveAsync(MutationKind.Delete, ct);

            if (mutation != DomainMutation.Deleted && mutation != DomainMutation.NoOp)
                throw new ConflictException("The lane could not be deleted due to a conflicting state.");
        }
    }
}
