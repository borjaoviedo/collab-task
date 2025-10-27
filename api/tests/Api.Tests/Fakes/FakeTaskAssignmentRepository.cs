using Application.TaskAssignments.Abstractions;
using Application.TaskAssignments.Changes;
using Domain.Entities;
using Domain.Enums;

namespace Api.Tests.Fakes
{
    public sealed class FakeTaskAssignmentRepository : ITaskAssignmentRepository
    {
        private readonly Dictionary<(Guid TaskId, Guid UserId), TaskAssignment> _map = [];

        private static byte[] NextRowVersion() => Guid.NewGuid().ToByteArray();

        public Task<TaskAssignment?> GetByTaskAndUserIdAsync(Guid taskId, Guid userId, CancellationToken ct = default)
            => Task.FromResult(_map.TryGetValue((taskId, userId), out var a) ? Clone(a) : null);

        public Task<TaskAssignment?> GetTrackedByTaskAndUserIdAsync(Guid taskId, Guid userId, CancellationToken ct = default)
            => Task.FromResult(_map.TryGetValue((taskId, userId), out var a) ? a : null);

        public Task<IReadOnlyList<TaskAssignment>> ListByTaskAsync(Guid taskId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<TaskAssignment>>(_map.Values.Where(a => a.TaskId == taskId).Select(Clone).ToList());

        public Task<IReadOnlyList<TaskAssignment>> ListByUserAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<TaskAssignment>>(_map.Values.Where(a => a.UserId == userId).Select(Clone).ToList());

        public Task<bool> ExistsAsync(Guid taskId, Guid userId, CancellationToken ct = default)
            => Task.FromResult(_map.ContainsKey((taskId, userId)));

        public Task<bool> AnyOwnerAsync(Guid taskId, Guid? excludeUserId = null, CancellationToken ct = default)
            => Task.FromResult(_map.Values.Any(a => a.TaskId == taskId
                                                    && a.Role == TaskRole.Owner
                                                    && (excludeUserId == null || a.UserId != excludeUserId)));
        public Task AddAsync(TaskAssignment assignment, CancellationToken ct = default)
        {
            assignment.SetRowVersion(NextRowVersion());
            _map[(assignment.TaskId, assignment.UserId)] = assignment;
            return Task.CompletedTask;
        }

        public Task<(PrecheckStatus Status, AssignmentChange? Change)> ChangeRoleAsync(
            Guid taskId,
            Guid userId,
            TaskRole newRole,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            if (!_map.TryGetValue((taskId, userId), out var existing))
                return Task.FromResult((PrecheckStatus.NotFound, (AssignmentChange?)null));
            if (existing.Role == newRole)
                return Task.FromResult((PrecheckStatus.NoOp, (AssignmentChange?)null));

            if (newRole == TaskRole.Owner &&
                _map.Values.Any(a => a.TaskId == taskId && a.UserId != userId && a.Role == TaskRole.Owner))
                return Task.FromResult((PrecheckStatus.Conflict, (AssignmentChange?)null));

            if (!existing.RowVersion.SequenceEqual(rowVersion))
                return Task.FromResult((PrecheckStatus.Conflict, (AssignmentChange?)null));

            existing.SetRole(newRole);
            existing.SetRowVersion(NextRowVersion());
            return Task.FromResult((PrecheckStatus.Ready, (AssignmentChange?)null));
        }

        public Task<PrecheckStatus> DeleteAsync(Guid taskId, Guid userId, byte[] rowVersion, CancellationToken ct = default)
        {
            if (!_map.TryGetValue((taskId, userId), out var existing))
                return Task.FromResult(PrecheckStatus.NotFound);
            if (!existing.RowVersion.SequenceEqual(rowVersion))
                return Task.FromResult(PrecheckStatus.Conflict);

            _map.Remove((taskId, userId));
            return Task.FromResult(PrecheckStatus.Ready);
        }

        private static TaskAssignment Clone(TaskAssignment a)
        {
            var clone = TaskAssignment.Create(a.TaskId, a.UserId, a.Role);
            var rowVersion = (a.RowVersion is null) ? [] : a.RowVersion.ToArray();
            clone.SetRowVersion(rowVersion);
            return clone;
        }
    }
}
