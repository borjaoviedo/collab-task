
namespace Application.Common.Exceptions
{
    public sealed class PreconditionFailedException : BaseAppException
    {
        public PreconditionFailedException(
            string message,
            string? code = "precondition_failed") : base(message, code) { }
    }
}
