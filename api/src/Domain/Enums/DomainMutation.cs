namespace Domain.Enums
{
    public enum DomainMutation
    {
        NoOp = 0,
        NotFound = 1,
        Updated = 2,
        Created = 3,
        Deleted = 4,
        Conflict = 5
    }
}
