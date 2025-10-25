using Domain.Common;
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
            Guards.NotEmpty(projectId);

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
            Guards.NonNegative(order);
            if (Order == order) return;

            Order = order;
        }

        internal void SetRowVersion(byte[] rowVersion)
        {
            Guards.NotNull(rowVersion);
            RowVersion = rowVersion;
        }
    }
}
