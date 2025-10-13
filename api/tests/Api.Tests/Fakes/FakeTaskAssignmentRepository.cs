using Application.TaskAssignments.Abstractions;
using Domain.Entities;
using Domain.Enums;

namespace Api.Tests.Fakes
{
    public sealed class FakeTaskAssignmentRepository : ITaskAssignmentRepository
    {
        private readonly Dictionary<(Guid TaskId, Guid UserId), TaskAssignment> _map = [];

        private static byte[] NextRowVersion() => Guid.NewGuid().ToByteArray();

        public Task<TaskAssignment?> GetAsync(Guid taskId, Guid userId, CancellationToken ct = default)
            => Task.FromResult(_map.TryGetValue((taskId, userId), out var a) ? Clone(a) : null);

        public Task<TaskAssignment?> GetTrackedAsync(Guid taskId, Guid userId, CancellationToken ct = default)
            => Task.FromResult(_map.TryGetValue((taskId, userId), out var a) ? a : null);

        public Task<IReadOnlyList<TaskAssignment>> ListByTaskAsync(Guid taskId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<TaskAssignment>>(_map.Values.Where(a => a.TaskId == taskId).Select(Clone).ToList());

        public Task<IReadOnlyList<TaskAssignment>> ListByUserAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<TaskAssignment>>(_map.Values.Where(a => a.UserId == userId).Select(Clone).ToList());

        public Task<bool> ExistsAsync(Guid taskId, Guid userId, CancellationToken ct = default)
            => Task.FromResult(_map.ContainsKey((taskId, userId)));

        public Task<bool> AnyOwnerAsync(Guid taskId, CancellationToken ct = default)
            => Task.FromResult(_map.Values.Any(a => a.TaskId == taskId && a.Role == TaskRole.Owner));

        public Task<int> CountByRoleAsync(Guid taskId, TaskRole role, CancellationToken ct = default)
            => Task.FromResult(_map.Values.Count(a => a.TaskId == taskId && a.Role == role));

        public Task AddAsync(TaskAssignment assignment, CancellationToken ct = default)
        {
            assignment.RowVersion = NextRowVersion();
            _map[(assignment.TaskId, assignment.UserId)] = assignment;
            return Task.CompletedTask;
        }

        public Task<DomainMutation> AssignAsync(Guid taskId, Guid userId, TaskRole role, CancellationToken ct = default)
        {
            if (_map.TryGetValue((taskId, userId), out var existing))
            {
                if (existing.Role == role)
                    return Task.FromResult(DomainMutation.NoOp);

                if (role == TaskRole.Owner &&
                    _map.Values.Any(a => a.TaskId == taskId && a.UserId != userId && a.Role == TaskRole.Owner))
                    return Task.FromResult(DomainMutation.Conflict);

                existing.Role = role;
                existing.RowVersion = NextRowVersion();
                return Task.FromResult(DomainMutation.Updated);
            }

            if (role == TaskRole.Owner &&
                _map.Values.Any(a => a.TaskId == taskId && a.Role == TaskRole.Owner))
                return Task.FromResult(DomainMutation.Conflict);

            var aNew = TaskAssignment.Create(taskId, userId, role);
            aNew.RowVersion = NextRowVersion();
            _map[(taskId, userId)] = aNew;
            return Task.FromResult(DomainMutation.Created);
        }

        public Task<DomainMutation> ChangeRoleAsync(Guid taskId, Guid userId, TaskRole newRole, byte[] rowVersion, CancellationToken ct = default)
        {
            if (!_map.TryGetValue((taskId, userId), out var existing))
                return Task.FromResult(DomainMutation.NotFound);
            if (existing.Role == newRole)
                return Task.FromResult(DomainMutation.NoOp);

            if (newRole == TaskRole.Owner &&
                _map.Values.Any(a => a.TaskId == taskId && a.UserId != userId && a.Role == TaskRole.Owner))
                return Task.FromResult(DomainMutation.Conflict);

            if (!existing.RowVersion.SequenceEqual(rowVersion))
                return Task.FromResult(DomainMutation.Conflict);

            existing.Role = newRole;
            existing.RowVersion = NextRowVersion();
            return Task.FromResult(DomainMutation.Updated);
        }

        public Task<DomainMutation> RemoveAsync(Guid taskId, Guid userId, byte[] rowVersion, CancellationToken ct = default)
        {
            if (!_map.TryGetValue((taskId, userId), out var existing))
                return Task.FromResult(DomainMutation.NotFound);
            if (!existing.RowVersion.SequenceEqual(rowVersion))
                return Task.FromResult(DomainMutation.Conflict);

            _map.Remove((taskId, userId));
            return Task.FromResult(DomainMutation.Deleted);
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);

        private static TaskAssignment Clone(TaskAssignment a)
        {
            var clone = TaskAssignment.Create(a.TaskId, a.UserId, a.Role);
            clone.RowVersion = (a.RowVersion is null) ? Array.Empty<byte>() : a.RowVersion.ToArray();
            return clone;
        }
    }
}
