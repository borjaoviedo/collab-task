using Application.Abstractions.Persistence;
using Application.Columns.Abstractions;
using Application.Columns.DTOs;
using Application.Columns.Mapping;
using Application.Common.Exceptions;
using Application.Projects.Mapping;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Columns.Services
{
    /// <summary>
    /// Application write-side service for <see cref="Column"/> aggregates.
    /// Handles creation, renaming, deletion, and two-phase reordering of columns within a lane,
    /// enforcing name uniqueness, board consistency rules, and optimistic concurrency guarantees.
    /// All changes are persisted atomically through <see cref="IUnitOfWork"/> to ensure that
    /// updates to the project board remain consistent even under concurrent access.
    /// </summary>
    /// <param name="columnRepository">
    /// Repository responsible for querying, tracking, and mutating <see cref="Column"/> entities,
    /// including name uniqueness checks and the two-phase reorder algorithm through
    /// <c>PrepareReorderAsync</c> and <c>FinalizeReorderAsync</c>.
    /// </param>
    /// <param name="unitOfWork">
    /// Coordinates transactional persistence and evaluates <see cref="DomainMutation"/> results,
    /// guaranteeing that column write operations complete successfully or fail safely when
    /// optimistic concurrency conflicts occur.
    /// </param>
    public sealed class ColumnWriteService(
        IColumnRepository columnRepository,
        IUnitOfWork unitOfWork) : IColumnWriteService
    {
        private readonly IColumnRepository _columnRepository = columnRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        /// <inheritdoc/>
        public async Task<ColumnReadDto> CreateAsync(
            Guid projectId,
            Guid laneId,
            ColumnCreateDto dto,
            CancellationToken ct = default)
        {
            var columnNameVo = ColumnName.Create(dto.Name);
            if (await _columnRepository.ExistsWithNameAsync(laneId, columnNameVo, ct: ct))
                throw new ConflictException("A column with the specified name already exists.");

            var column = Column.Create(projectId, laneId, columnNameVo, dto.Order);
            await _columnRepository.AddAsync(column, ct);

            var mutation = await _unitOfWork.SaveAsync(MutationKind.Create, ct);
            if (mutation != DomainMutation.Created)
                throw new ConflictException("Column could not be created due to a conflicting state.");

            return column.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task<ColumnReadDto> RenameAsync(
            Guid columnId,
            ColumnRenameDto dto,
            CancellationToken ct = default)
        {
            var column = await _columnRepository.GetByIdForUpdateAsync(columnId, ct)
                ?? throw new NotFoundException("Column not found.");

            var newNameVo = ColumnName.Create(dto.NewName);
            column.Rename(newNameVo);

            var mutation = await _unitOfWork.SaveAsync(MutationKind.Update, ct);
            if (mutation != DomainMutation.Updated)
                throw new ConflictException("The column name could not be changed due to a conflicting state.");

            return column.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task<ColumnReadDto> ReorderAsync(
            Guid columnId,
            ColumnReorderDto dto,
            CancellationToken ct = default)
        {
            var precheck = await _columnRepository.PrepareReorderAsync(
                columnId,
                dto.NewOrder,
                ct);

            if (precheck is PrecheckStatus.NotFound)
                throw new NotFoundException("Column not found.");

            if (precheck is PrecheckStatus.NoOp)
            {
                var current = await _columnRepository.GetByIdAsync(columnId, ct)
                    ?? throw new NotFoundException("Column not found.");

                return current.ToReadDto();
            }

            var firstMutation = await _unitOfWork.SaveAsync(MutationKind.Update, ct);
            if (firstMutation == DomainMutation.Conflict)
                throw new ConflictException("The column could not be reordered due to a conflicting state.");

            await _columnRepository.FinalizeReorderAsync(columnId, ct);

            var secondMutation = await _unitOfWork.SaveAsync(MutationKind.Update, ct);
            if (secondMutation != DomainMutation.Updated)
                throw new ConflictException("The column could not be reordered due to a conflicting state.");

            var reloaded = await _columnRepository.GetByIdAsync(columnId, ct)
                ?? throw new NotFoundException("Column not found after reorder.");

            return reloaded.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task DeleteByIdAsync(Guid columnId,CancellationToken ct = default)
        {
            var column = await _columnRepository.GetByIdForUpdateAsync(columnId, ct)
                ?? throw new NotFoundException("Column not found.");

            await _columnRepository.RemoveAsync(column, ct);
            var mutation = await _unitOfWork.SaveAsync(MutationKind.Delete, ct);

            if (mutation != DomainMutation.Deleted && mutation != DomainMutation.NoOp)
                throw new ConflictException("The column could not be deleted due to a conflicting state.");
        }
    }
}
