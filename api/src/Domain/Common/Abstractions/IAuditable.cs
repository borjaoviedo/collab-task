
namespace Domain.Common.Abstractions
{
    public interface IAuditable
    {
        DateTimeOffset CreatedAt { get; }
        DateTimeOffset UpdatedAt { get; }
    }
}
