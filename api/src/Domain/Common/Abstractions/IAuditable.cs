
namespace Domain.Common.Abstractions
{
    /// <summary>
    /// Defines audit metadata for domain entities.
    /// </summary>
    public interface IAuditable
    {
        /// <summary>
        /// Gets the UTC timestamp when the entity was first created.
        /// </summary>
        DateTimeOffset CreatedAt { get; }

        /// <summary>
        /// Gets the UTC timestamp when the entity was last modified.
        /// </summary>
        DateTimeOffset UpdatedAt { get; }
    }
}
