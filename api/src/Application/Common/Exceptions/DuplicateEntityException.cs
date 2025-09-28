
namespace Application.Common.Exceptions
{
    public sealed class DuplicateEntityException : BaseAppException
    {
        public DuplicateEntityException(
            string message,
            string? code = "duplicate_entity") : base(message, code) { }
    }
}
