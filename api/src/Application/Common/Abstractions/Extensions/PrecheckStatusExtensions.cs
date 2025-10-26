using Domain.Enums;

namespace Application.Common.Abstractions.Extensions
{
    public static class PrecheckStatusExtensions
    {
        public static DomainMutation ToErrorDomainMutation(this PrecheckStatus status) =>
         status switch
         {
             PrecheckStatus.NotFound => DomainMutation.NotFound,
             PrecheckStatus.NoOp => DomainMutation.NoOp,
             PrecheckStatus.Conflict => DomainMutation.Conflict,
             _ => throw new InvalidOperationException($"Unexpected status: {status}")
         };
    }
}
