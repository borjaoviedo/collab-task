using Domain.ValueObjects;
using Domain.Common;

namespace Domain.Entities
{
    /// <summary>
    /// Represents a column within a lane of a project board.
    /// </summary>
    public sealed class Column
    {
        public Guid Id { get; private set; }
        public Guid LaneId { get; private set; }
        public Guid ProjectId { get; private set; }
        public ColumnName Name { get; private set; } = default!;
        public int Order { get; private set; }
        public byte[] RowVersion { get; private set; } = default!;

        private Column() { }

        /// <summary>Creates a new column within a lane.</summary>
        public static Column Create(Guid projectId, Guid laneId, ColumnName name, int? order)
        {
            Guards.NotEmpty(projectId);
            Guards.NotEmpty(laneId);

            return new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                LaneId = laneId,
                Name = name,
                Order = Math.Max(0, order ?? 0)
            };
        }

        /// <summary>Renames the column if the provided name differs from the current one.</summary>
        public void Rename(ColumnName name)
        {
            if (Name.Equals(name)) return;
            Name = name;
        }

        /// <summary>Updates the column order if different from the current one.</summary>
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
