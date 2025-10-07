
namespace Domain.Common.Exceptions
{
    public abstract class DomainException : Exception
    {
        public string? Code { get; }

        protected DomainException(string message, string? code = null) : base(message) => Code = code;
    }
}
