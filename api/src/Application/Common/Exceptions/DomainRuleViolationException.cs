
namespace Application.Common.Exceptions
{
    public sealed class DomainRuleViolationException : BaseAppException
    {
        public DomainRuleViolationException(
            string message,
            string? code = "domain_rule_violation") : base(message, code) { }
    }
}
