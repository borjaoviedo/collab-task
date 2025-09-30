
namespace Application.Common.Exceptions
{
    public sealed class ForbiddenAccessException : BaseAppException
    {
        public ForbiddenAccessException(
            string message,
            string? code = "forbidden") : base(message, code) { }
    }
}
