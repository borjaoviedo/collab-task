
namespace Application.Common.Exceptions
{
    public sealed class ValidationException : BaseAppException
    {
        public ValidationException(
            string message,
            string? code = "validation_error") : base(message, code) { }
    }
}
