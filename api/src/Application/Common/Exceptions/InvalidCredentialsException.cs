
namespace Application.Common.Exceptions
{
    public sealed class InvalidCredentialsException : BaseAppException
    {
        public InvalidCredentialsException(
            string message,
            string? code = "invalid_credentials") : base(message, code) { }
    }
}
