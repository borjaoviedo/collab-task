using Domain.Common.Abstractions;
using Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public sealed class TaskItem : IAuditable
    {
        public Guid Id { get; set; }
        public Guid ColumnId { get; set; }
        public Guid LaneId { get; set; }
        public Guid ProjectId { get; set; }
        public required TaskTitle Title { get; set; }
        public required TaskDescription Description { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        public decimal SortKey { get; set; }
        [Timestamp] public byte[] RowVersion { get; set; } = default!;

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

        public void Move(Guid targetLaneId, Guid targetColumnId, decimal targetSortKey)
        {
            if (targetLaneId == Guid.Empty) throw new ArgumentException("LaneId cannot be empty.", nameof(targetLaneId));
            if (targetColumnId == Guid.Empty) throw new ArgumentException("ColumnId cannot be empty.", nameof(targetColumnId));

            LaneId = targetLaneId;
            ColumnId = targetColumnId;
            SortKey = targetSortKey;
        }
    }
}
