
namespace Application.Common.Exceptions
{
    public sealed class ConcurrencyException : BaseAppException
    {
        public ConcurrencyException(
            string message,
            string? code = "concurrency_conflict") : base(message, code) { }
    }
}
