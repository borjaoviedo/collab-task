using Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public sealed class Lane
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public required LaneName Name { get; set; }
        public int Order { get; set; }
        [Timestamp] public byte[] RowVersion { get; set; } = default!;

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
            if (order < 0) throw new ArgumentOutOfRangeException(nameof(order), "Order must be greater than 0.");
            if (Order == order) return;
            Order = order;
        }
    }
}
