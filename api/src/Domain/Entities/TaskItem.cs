using Domain.Common.Abstractions;
using Domain.ValueObjects;

namespace Domain.Entities
{
    public sealed class TaskItem : IAuditable
    {
        public Guid Id { get; private set; }
        public Guid ColumnId { get; private set; }
        public Guid LaneId { get; private set; }
        public Guid ProjectId { get; private set; }
        public TaskTitle Title { get; private set; } = default!;
        public TaskDescription Description { get; private set; } = default!;
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset UpdatedAt { get; private set; }
        public DateTimeOffset? DueDate { get; private set; }
        public decimal SortKey { get; private set; }
        public byte[] RowVersion { get; private set; } = default!;

        private TaskItem() { }

        public static TaskItem Create(
            Guid columnId,
            Guid laneId,
            Guid projectId,
            TaskTitle title,
            TaskDescription description,
            DateTimeOffset? dueDate = null,
            decimal? sortKey = null)
        {
            if (projectId == Guid.Empty) throw new ArgumentException("ProjectId cannot be empty.", nameof(projectId));
            if (laneId == Guid.Empty) throw new ArgumentException("LaneId cannot be empty.", nameof(laneId));
            if (columnId == Guid.Empty) throw new ArgumentException("ColumnId cannot be empty.", nameof(columnId));
            if (dueDate is not null && dueDate < DateTimeOffset.UtcNow)
                throw new ArgumentException("Due date cannot be in the past.", nameof(dueDate));

            return new TaskItem
            {
                Id = Guid.NewGuid(),
                ColumnId = columnId,
                LaneId = laneId,
                ProjectId = projectId,
                Title = title,
                Description = description,
                DueDate = dueDate,
                SortKey = sortKey ?? 0m
            };
        }

        public void Edit(TaskTitle? title, TaskDescription? description, DateTimeOffset? dueDate)
        {
            if (title is not null && !Title.Equals(title)) Title = title;
            if (description is not null && !Description.Equals(description)) Description = description;
            if (dueDate is not null && dueDate < DateTimeOffset.UtcNow)
                throw new ArgumentException("Due date cannot be in the past.", nameof(dueDate));

            if (DueDate != dueDate) DueDate = dueDate;
        }

        public void Move(Guid targetProject, Guid targetLaneId, Guid targetColumnId, decimal targetSortKey)
        {
            if (targetProject == Guid.Empty) throw new ArgumentException("ProjectId cannot be empty.", nameof(targetProject));
            if (targetProject != ProjectId) throw new ArgumentException("Move must stay within the same Project.", nameof(targetProject));
            if (targetLaneId == Guid.Empty) throw new ArgumentException("LaneId cannot be empty.", nameof(targetLaneId));
            if (targetColumnId == Guid.Empty) throw new ArgumentException("ColumnId cannot be empty.", nameof(targetColumnId));
            if (targetSortKey < 0m) throw new ArgumentOutOfRangeException(nameof(targetSortKey), "SortKey must be equal or greater than 0.");

            if (LaneId == targetLaneId && ColumnId == targetColumnId && SortKey == targetSortKey) return;

            LaneId = targetLaneId;
            ColumnId = targetColumnId;
            SortKey = targetSortKey;
        }

        internal void SetRowVersion(byte[] value)
            => RowVersion = value ?? throw new ArgumentNullException(nameof(value));

        internal void SetSortKey(decimal sortKey) => SortKey = sortKey;
    }
}
