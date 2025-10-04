
namespace Domain.Common.Exceptions
{
    public sealed class DomainRuleViolationException : DomainException
    {
        public DomainRuleViolationException(
            string message,
            string? code = "domain_rule_violation") : base(message, code) { }
    }
}
