using Domain.Common.Abstractions;
using Domain.ValueObjects;

namespace Domain.Entities
{
    public sealed class TaskNote : IAuditable
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid AuthorId { get; set; }
        public required NoteContent Content { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = default!;

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
    }
}
