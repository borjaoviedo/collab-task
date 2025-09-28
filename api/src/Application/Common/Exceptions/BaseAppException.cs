
namespace Application.Common.Exceptions
{
    public abstract class BaseAppException : Exception
    {
        public string? Code { get; }

        protected BaseAppException(string message, string? code = null) : base(message) => Code = code;
    }
}
