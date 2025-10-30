using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Entities
{
    /// <summary>
    /// Represents a lane within a project board.
    /// </summary>
    public sealed class Lane
    {
        public Guid Id { get; private set; }
        public Guid ProjectId { get; private set; }
        public LaneName Name { get; private set; } = default!;
        public int Order { get; private set; }
        public byte[] RowVersion { get; private set; } = default!;

        private Lane() { }

        /// <summary>Creates a new lane within a project.</summary>
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

        /// <summary>Renames the lane if the provided name differs from the current one.</summary>
        public void Rename(LaneName name)
        {
            if (Name.Equals(name)) return;
            Name = name;
        }

        /// <summary>Updates the lane order if different from the current one.</summary>
        public void Reorder(int order)
        {
            Guards.NonNegative(order);
            if (Order == order) return;
            Order = order;
        }

        /// <summary>Sets the concurrency token after persistence.</summary>
        internal void SetRowVersion(byte[] rowVersion)
        {
            Guards.NotNull(rowVersion);
            RowVersion = rowVersion;
        }
    }
}
