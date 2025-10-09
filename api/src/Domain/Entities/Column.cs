using Domain.Common.Abstractions;
using Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public sealed class Column : IAuditable
    {
        public Guid Id { get; set; }
        public Guid LaneId { get; set; }
        public Guid ProjectId { get; set; }
        public required ColumnName Name { get; set; }
        public int Order { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        [Timestamp] public byte[] RowVersion { get; set; } = default!;

        private Column() { }

        public static Column Create(Guid projectId, Guid laneId, ColumnName name, int? order)
            => new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                LaneId = laneId,
                Name = name,
                Order = Math.Max(0, order ?? 0)
            };

        public void Rename(ColumnName name)
        {
            if (Name.Equals(name)) return;
            Name = name;
        }

        public void Reorder(int order)
        {
            if (order < 0) throw new ArgumentOutOfRangeException(nameof(order), "Order must be greater than 0.");
            if (Order == order) return;
            Order = order;
        }
    }
}
