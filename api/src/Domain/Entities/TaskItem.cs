using Domain.Common;
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
            Guards.NotEmpty(projectId, nameof(projectId));
            Guards.NotEmpty(laneId, nameof(laneId));
            Guards.NotEmpty(columnId, nameof(columnId));
            Guards.NotInPast(dueDate, nameof(dueDate));

            if (sortKey is not null) Guards.NonNegative((decimal)sortKey, nameof(sortKey));

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
            Guards.NotInPast(dueDate, nameof(dueDate));

            if (title is not null && !Title.Equals(title)) Title = title;
            if (description is not null && !Description.Equals(description)) Description = description;
            if (DueDate != dueDate) DueDate = dueDate;
        }

        public void Move(Guid targetProject, Guid targetLaneId, Guid targetColumnId, decimal targetSortKey)
        {
            if (targetProject != ProjectId)
                throw new ArgumentException("Move must stay within the same Project.", nameof(targetProject));

            Guards.NotEmpty(targetProject, nameof(targetProject));
            Guards.NotEmpty(targetLaneId, nameof(targetLaneId));
            Guards.NotEmpty(targetColumnId, nameof(targetColumnId));
            Guards.NonNegative(targetSortKey, nameof(targetSortKey));

            if (LaneId == targetLaneId && ColumnId == targetColumnId && SortKey == targetSortKey) return;

            LaneId = targetLaneId;
            ColumnId = targetColumnId;
            SortKey = targetSortKey;
        }

        internal void SetRowVersion(byte[] rowVersion)
        {
            Guards.NotNull(rowVersion, nameof(rowVersion));
            RowVersion = rowVersion;
        }

        internal void SetSortKey(decimal sortKey)
        {
            Guards.NonNegative(sortKey, nameof(sortKey));
            SortKey = sortKey;
        }
    }
}
