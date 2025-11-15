using Application.Common.Exceptions;
using Application.TaskNotes.Abstractions;
using Application.TaskNotes.DTOs;
using Application.TaskNotes.Mapping;

namespace Application.TaskNotes.Services
{
    /// <summary>
    /// Application read-side service for <see cref="Domain.Entities.TaskNote"/> aggregates.
    /// Provides high-level query operations for retrieving a single note, listing all notes
    /// associated with a given task, and listing all notes authored by a particular user.
    /// Returned entities are mapped to <see cref="TaskNoteReadDto"/> to expose a stable,
    /// API-oriented representation. Missing notes are surfaced as
    /// <see cref="NotFoundException"/> to ensure consistent error handling.
    /// </summary>
    /// <param name="taskNoteRepository">
    /// Repository used for querying <see cref="Domain.Entities.TaskNote"/> entities,
    /// including lookups by identifier, lists by task, and lists by user.
    /// </param>
    public sealed class TaskNoteReadService(
        ITaskNoteRepository taskNoteRepository) : ITaskNoteReadService
    {
        private readonly ITaskNoteRepository _taskNoteRepository = taskNoteRepository;

        /// <inheritdoc/>
        public async Task<TaskNoteReadDto> GetByIdAsync(
            Guid noteId,
            CancellationToken ct = default)
        {
            var taskNote = await _taskNoteRepository.GetByIdAsync(noteId, ct)
                // 404 if the note does not exist
                ?? throw new NotFoundException("Task note not found.");

            return taskNote.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<TaskNoteReadDto>> ListByTaskIdAsync(
            Guid taskId,
            CancellationToken ct = default)
        {
            var taskNotes = await _taskNoteRepository.ListByTaskIdAsync(taskId, ct);

            return taskNotes
                .Select(tn => tn.ToReadDto())
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<TaskNoteReadDto>> ListByUserIdAsync(
            Guid userId,
            CancellationToken ct = default)
        {
            var taskNotes = await _taskNoteRepository.ListByUserIdAsync(userId, ct);

            return taskNotes
                .Select(tn => tn.ToReadDto())
                .ToList();
        }
    }
}
