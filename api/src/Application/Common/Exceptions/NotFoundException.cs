
namespace Application.Common.Exceptions
{
    public sealed class NotFoundException : BaseAppException
    {
        public NotFoundException(
            string message,
            string? code = "not_found") : base(message, code) { }
    }
}
