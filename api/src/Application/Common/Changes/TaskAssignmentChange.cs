using Domain.Enums;

namespace Application.Common.Changes
{
    public enum AssignmentChangeKind { RoleChanged }
    public abstract record AssignmentChange(AssignmentChangeKind Kind);
    public sealed record AssignmentRoleChangedChange(TaskRole OldRole, TaskRole NewRole)
        : AssignmentChange(AssignmentChangeKind.RoleChanged);
}
