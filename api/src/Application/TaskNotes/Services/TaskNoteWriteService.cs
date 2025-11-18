using Application.Abstractions.Auth;
using Application.Abstractions.Persistence;
using Application.Common.Exceptions;
using Application.TaskActivities.Abstractions;
using Application.TaskActivities.Payloads;
using Application.TaskNotes.Abstractions;
using Application.TaskNotes.DTOs;
using Application.TaskNotes.Mapping;
using Application.TaskNotes.Realtime;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using MediatR;

namespace Application.TaskNotes.Services
{
    /// <summary>
    /// Application write-side service for <see cref="TaskNote"/> aggregates.
    /// Handles creation, editing, and deletion of task-attached notes while enforcing
    /// project/task membership and note ownership rules. Each successful write operation
    /// persists changes via <see cref="IUnitOfWork"/>, records a corresponding
    /// <see cref="TaskActivity"/> entry, and publishes MediatR notifications
    /// so that other parts of the system can react to note lifecycle events.
    /// </summary>
    /// <param name="taskNoteRepository">
    /// Repository used for retrieving, tracking, and persisting <see cref="TaskNote"/> entities.
    /// </param>
    /// <param name="unitOfWork">
    /// Coordinates transactional persistence and maps <see cref="DomainMutation"/> outcomes
    /// to optimistic concurrency handling for note operations.
    /// </param>
    /// <param name="taskActivityWriteService">
    /// Service responsible for creating <see cref="TaskActivity"/> records that
    /// capture note-related events such as creation, edits, and deletions.
    /// </param>
    /// <param name="currentUserService">
    /// Provides information about the currently authenticated user, such as <c>UserId</c>.
    /// </param>
    /// <param name="mediator">
    /// MediatR abstraction used to publish note domain notifications so other components
    /// (for example, real-time hubs or background workers) can subscribe to note changes.
    /// </param>
    public sealed class TaskNoteWriteService(
        ITaskNoteRepository taskNoteRepository,
        IUnitOfWork unitOfWork,
        ITaskActivityWriteService taskActivityWriteService,
        ICurrentUserService currentUserService,
        IMediator mediator) : ITaskNoteWriteService
    {
        private readonly ITaskNoteRepository _taskNoteRepository = taskNoteRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ITaskActivityWriteService _taskActivityWriteService = taskActivityWriteService;
        private readonly ICurrentUserService _currentUserService = currentUserService;
        private readonly IMediator _mediator = mediator;

        /// <inheritdoc/>
        public async Task<TaskNoteReadDto> CreateAsync(
            Guid projectId,
            Guid taskId,
            TaskNoteCreateDto dto,
            CancellationToken ct = default)
        {
            var noteContentVo = NoteContent.Create(dto.Content);
            var currentUserId = (Guid)_currentUserService.UserId!;
            var note = TaskNote.Create(taskId, currentUserId, noteContentVo);

            await _taskNoteRepository.AddAsync(note, ct);

            var mutation = await _unitOfWork.SaveAsync(MutationKind.Create, ct);
            if (mutation != DomainMutation.Created)
                throw new ConflictException("Task note could not be created due to a conflicting state.");

            var payload = ActivityPayloadFactory.NoteAdded(note.Id);
            await _taskActivityWriteService.CreateAsync(
                taskId,
                currentUserId,
                TaskActivityType.NoteAdded,
                payload,
                ct);

            var notification = new TaskNoteCreated(
                projectId,
                new TaskNoteCreatedPayload(taskId, note.Id, noteContentVo.Value));
            await _mediator.Publish(notification, ct);

            return note.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task<TaskNoteReadDto> EditAsync(
            Guid projectId,
            Guid taskId,
            Guid noteId,
            TaskNoteEditDto dto,
            CancellationToken ct = default)
        {
            var note = await _taskNoteRepository.GetByIdForUpdateAsync(noteId, ct)
                ?? throw new NotFoundException("Task note not found.");

            var noteContentVo = NoteContent.Create(dto.NewContent);
            note.Edit(noteContentVo);

            var mutation = await _unitOfWork.SaveAsync(MutationKind.Update, ct);
            if (mutation != DomainMutation.Updated)
                throw new ConflictException("The task note could not be edited due to a conflicting state.");

            var currentUserId = (Guid)_currentUserService.UserId!;

            var payload = ActivityPayloadFactory.NoteEdited(noteId);
            await _taskActivityWriteService.CreateAsync(
                taskId,
                currentUserId,
                TaskActivityType.NoteEdited,
                payload,
                ct);

            var notification = new TaskNoteUpdated(
                    projectId,
                    new TaskNoteUpdatedPayload(taskId, noteId, noteContentVo.Value));
            await _mediator.Publish(notification, ct);

            return note.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(
            Guid projectId,
            Guid taskId,
            Guid noteId,
            CancellationToken ct = default)
        {
            var note = await _taskNoteRepository.GetByIdForUpdateAsync(noteId, ct)
                ?? throw new NotFoundException("Task note not found.");

            await _taskNoteRepository.RemoveAsync(note, ct);
            var mutation = await _unitOfWork.SaveAsync(MutationKind.Delete, ct);

            if (mutation != DomainMutation.Deleted && mutation != DomainMutation.NoOp)
                throw new ConflictException("The task note could not be deleted due to a conflicting state.");

            var notification = new TaskNoteDeleted(
                projectId,
                new TaskNoteDeletedPayload(taskId, noteId));
            await _mediator.Publish(notification, ct);
        }
    }
}
