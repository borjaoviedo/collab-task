
namespace Domain.Common.Exceptions
{
    public sealed class DuplicateEntityException : DomainException
    {
        public DuplicateEntityException(
            string message,
            string? code = "duplicate_entity") : base(message, code) { }
    }
}
