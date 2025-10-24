using Domain.ValueObjects;

namespace Domain.Entities
{
    public sealed class Column
    {
        public Guid Id { get; private set; }
        public Guid LaneId { get; private set; }
        public Guid ProjectId { get; private set; }
        public ColumnName Name { get; private set; } = default!;
        public int Order { get; private set; }
        public byte[] RowVersion { get; private set; } = default!;

        private Column() { }

        public static Column Create(Guid projectId, Guid laneId, ColumnName name, int? order)
        {
            if (projectId == Guid.Empty) throw new ArgumentException("ProjectId cannot be empty.", nameof(projectId));
            if (laneId == Guid.Empty) throw new ArgumentException("LaneId cannot be empty.", nameof(laneId));

            return new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                LaneId = laneId,
                Name = name,
                Order = Math.Max(0, order ?? 0)
            };
        }

        public void Rename(ColumnName name)
        {
            if (Name.Equals(name)) return;

            Name = name;
        }

        public void Reorder(int order)
        {
            if (order < 0) throw new ArgumentOutOfRangeException(nameof(order), "Order must be equal or greater than 0.");
            if (Order == order) return;

            Order = order;
        }

        internal void SetRowVersion(byte[] value)
            => RowVersion = value ?? throw new ArgumentNullException(nameof(value));
    }
}
