using Domain.Common;
using Domain.Common.Abstractions;
using Domain.ValueObjects;

namespace Domain.Entities
{
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

        public void Edit(NoteContent content)
        {
            if (Content.Equals(content)) return;
            Content = content;
        }

        internal void SetRowVersion(byte[] rowVersion)
        {
            Guards.NotNull(rowVersion);
            RowVersion = rowVersion;
        }
    }
}
