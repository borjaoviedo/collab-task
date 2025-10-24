using Domain.Common.Abstractions;
using Domain.ValueObjects;

namespace Domain.Entities
{
    public sealed class TaskNote : IAuditable
    {
        public Guid Id { get; private set; }
        public Guid TaskId { get; private set; }
        public Guid AuthorId { get; private set; }
        public NoteContent Content { get; private set; } = default!;
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset UpdatedAt { get; private set; }
        public byte[] RowVersion { get; private set; } = default!;

        private TaskNote() { }

        public static TaskNote Create(Guid taskId, Guid authorId, NoteContent content)
        {
            if (taskId == Guid.Empty) throw new ArgumentException("TaskId cannot be empty.", nameof(taskId));
            if (authorId == Guid.Empty) throw new ArgumentException("AuthorId cannot be empty.", nameof(authorId));

            return new TaskNote
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                AuthorId = authorId,
                Content = content
            };
        }

        public void Edit(NoteContent content)
        {
            if (Content.Equals(content)) return;
            Content = content;
        }

        internal void SetRowVersion(byte[] value)
            => RowVersion = value ?? throw new ArgumentNullException(nameof(value));
    }
}
