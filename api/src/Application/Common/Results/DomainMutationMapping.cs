using Domain.Enums;

namespace Application.Common.Results
{
    public static class DomainMutationMapping
    {
        public static WriteResult ToWriteResult(this DomainMutation m) => m switch
        {
            DomainMutation.NoOp => WriteResult.NoOp,
            DomainMutation.NotFound => WriteResult.NotFound,
            DomainMutation.Updated => WriteResult.Updated,
            DomainMutation.Created => WriteResult.Created,
            DomainMutation.Deleted => WriteResult.Deleted,
            DomainMutation.Conflict => WriteResult.Conflict,
            _ => WriteResult.Conflict
        };
    }
}
