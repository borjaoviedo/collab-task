using Domain.ValueObjects;

namespace Domain.Entities
{
    public sealed class Lane
    {
        public Guid Id { get; private set; }
        public Guid ProjectId { get; private set; }
        public LaneName Name { get; private set; } = default!;
        public int Order { get; private set; }
        public byte[] RowVersion { get; private set; } = default!;

        private Lane() { }

        public static Lane Create(Guid projectId, LaneName name, int? order)
        {
            if (projectId == Guid.Empty) throw new ArgumentException("ProjectId cannot be empty.", nameof(projectId));

            return new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Name = name,
                Order = Math.Max(0, order ?? 0)
            };
        }

        public void Rename(LaneName name)
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
