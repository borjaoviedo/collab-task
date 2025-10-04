
namespace Domain.Common.Exceptions
{
    public sealed class EntityNotFoundException : DomainException
    {
        public EntityNotFoundException(
            string message,
            string? code = "entity_not_found") : base(message, code) { }
    }
}
