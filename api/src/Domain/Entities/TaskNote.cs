using Domain.Common;
using Domain.Common.Abstractions;
using Domain.ValueObjects;

namespace Domain.Entities
{
    /// <summary>
    /// Represents a note added by a user to a task.
    /// </summary>
    public sealed class TaskNote : IAuditable
    {
        public Guid Id { get; private set; }
        public Guid TaskId { get; private set; }
        public Guid UserId { get; private set; }
        public NoteContent Content { get; private set; } = default!;
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset UpdatedAt { get; private set; }
        public byte[] RowVersion { get; private set; } = default!;

        private TaskNote() { }

        /// <summary>
        /// Creates a new note for a given task and user.
        /// </summary>
        public static TaskNote Create(Guid taskId, Guid userId, NoteContent content)
        {
            Guards.NotEmpty(taskId);
            Guards.NotEmpty(userId);

            return new TaskNote
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = userId,
                Content = content
            };
        }

        /// <summary>
        /// Updates the note content if changed.
        /// </summary>
        public void Edit(NoteContent content)
        {
            if (Content.Equals(content)) return;
            Content = content;
        }

        /// <summary>Sets the concurrency token after persistence.</summary>
        internal void SetRowVersion(byte[] rowVersion)
        {
            Guards.NotNull(rowVersion);
            RowVersion = rowVersion;
        }
    }
}
