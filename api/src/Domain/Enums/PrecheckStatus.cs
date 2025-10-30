
namespace Domain.Enums
{
    /// <summary>
    /// Represents the result of a precheck phase before a mutation is executed.
    /// </summary>
    public enum PrecheckStatus
    {
        NotFound,
        NoOp,
        Conflict,
        Ready
    }
}
