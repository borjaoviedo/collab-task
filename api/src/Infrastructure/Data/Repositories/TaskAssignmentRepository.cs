using Application.TaskAssignments.Abstractions;
using Application.TaskAssignments.Changes;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Infrastructure.Data.Repositories
{
    public sealed class TaskAssignmentRepository(AppDbContext db) : ITaskAssignmentRepository
    {
        private readonly AppDbContext _db = db;

        public async Task<TaskAssignment?> GetAsync(Guid taskId, Guid userId, CancellationToken ct = default)
            => await _db.TaskAssignments
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.TaskId == taskId && a.UserId == userId, ct);

        public async Task<TaskAssignment?> GetTrackedAsync(Guid taskId, Guid userId, CancellationToken ct = default)
            => await _db.TaskAssignments
                        .FirstOrDefaultAsync(a => a.TaskId == taskId && a.UserId == userId, ct);
        public async Task<IReadOnlyList<TaskAssignment>> ListByTaskAsync(Guid taskId, CancellationToken ct = default)
            => await _db.TaskAssignments
                        .AsNoTracking()
                        .Where(a => a.TaskId == taskId)
                        .ToListAsync(ct);
        public async Task<IReadOnlyList<TaskAssignment>> ListByUserAsync(Guid userId, CancellationToken ct = default)
            => await _db.TaskAssignments
                        .AsNoTracking()
                        .Where(a => a.UserId == userId)
                        .ToListAsync(ct);
        public async Task<bool> ExistsAsync(Guid taskId, Guid userId, CancellationToken ct = default)
            => await _db.TaskAssignments
                        .AsNoTracking()
                        .AnyAsync(a => a.TaskId == taskId && a.UserId == userId, ct);
        public async Task<bool> AnyOwnerAsync(Guid taskId, CancellationToken ct = default)
            => await _db.TaskAssignments
                        .AsNoTracking()
                        .AnyAsync(a => a.TaskId == taskId && a.Role == TaskRole.Owner, ct);
        public async Task<int> CountByRoleAsync(Guid taskId, TaskRole role, CancellationToken ct = default)
            => await _db.TaskAssignments
                        .AsNoTracking()
                        .Where(a => a.TaskId == taskId && a.Role == role)
                        .CountAsync(ct);

        public async Task AddAsync(TaskAssignment assignment, CancellationToken ct = default)
            => await _db.TaskAssignments.AddAsync(assignment, ct);

        public async Task<(DomainMutation Mutation, AssignmentChange? Change)> AssignAsync(Guid taskId, Guid userId, TaskRole role, CancellationToken ct = default)
        {
            var existing = await GetTrackedAsync(taskId, userId, ct);
            if (existing is not null)
            {
                if (existing.Role == role) return (DomainMutation.NoOp, null);

                if (role == TaskRole.Owner)
                {
                    var anotherOwner = await _db.TaskAssignments.AsNoTracking()
                        .AnyAsync(a => a.TaskId == taskId && a.UserId != userId && a.Role == TaskRole.Owner, ct);
                    if (anotherOwner) return (DomainMutation.Conflict, null);
                }

                var change = new AssignmentRoleChangedChange(existing.Role, role);
                _db.Entry(existing).Property(a => a.Role).CurrentValue = role;
                _db.Entry(existing).Property(a => a.Role).IsModified = true;

                return (DomainMutation.Updated, change);
            }

            // Creating new assignment
            if (role == TaskRole.Owner)
            {
                var hasOwner = await AnyOwnerAsync(taskId, ct);
                if (hasOwner) return (DomainMutation.Conflict, null);
            }

            var assignment = TaskAssignment.Create(taskId, userId, role);
            _db.TaskAssignments.Add(assignment);
            return (DomainMutation.Created, null);
        }

        public async Task<(DomainMutation Mutation, AssignmentChange? Change)> ChangeRoleAsync(Guid taskId, Guid userId, TaskRole newRole, byte[] rowVersion, CancellationToken ct = default)
        {
            var existing = await GetTrackedAsync(taskId, userId, ct);
            if (existing is null) return (DomainMutation.NotFound, null);
            if (existing.Role == newRole) return (DomainMutation.NoOp, null);

            if (newRole == TaskRole.Owner)
            {
                var anotherOwner = await _db.TaskAssignments.AsNoTracking()
                    .AnyAsync(a => a.TaskId == taskId && a.UserId != userId && a.Role == TaskRole.Owner, ct);
                if (anotherOwner) return (DomainMutation.Conflict, null);
            }
            _db.Entry(existing).Property(a => a.RowVersion).OriginalValue = rowVersion;

            var change = new AssignmentRoleChangedChange(existing.Role, newRole);
            _db.Entry(existing).Property(a => a.Role).CurrentValue = newRole;
            _db.Entry(existing).Property(a => a.Role).IsModified = true;

            return (DomainMutation.Updated, change);
        }

        public async Task<DomainMutation> RemoveAsync(Guid taskId, Guid userId, byte[] rowVersion, CancellationToken ct = default)
        {
            var existing = await GetTrackedAsync(taskId, userId, ct);
            if (existing is null) return DomainMutation.NotFound;

            _db.Entry(existing).Property(a => a.RowVersion).OriginalValue = rowVersion;
            _db.TaskAssignments.Remove(existing);
            return DomainMutation.Deleted;
        }

        public async Task<int> SaveCreateChangesAsync(CancellationToken ct = default) => await _db.SaveChangesAsync(ct);

        public async Task<DomainMutation> SaveUpdateChangesAsync(CancellationToken ct = default)
        {
            try
            {
                await _db.SaveChangesAsync(ct);
                return DomainMutation.Updated;
            }
            catch (DbUpdateConcurrencyException)
            {
                return DomainMutation.Conflict;
            }
        }

        public async Task<DomainMutation> SaveRemoveChangesAsync(CancellationToken ct = default)
        {
            try
            {
                await _db.SaveChangesAsync(ct);
                return DomainMutation.Deleted;
            }
            catch (DbUpdateConcurrencyException)
            {
                return DomainMutation.Conflict;
            }
            catch (DbUpdateException)
            {
                return DomainMutation.Conflict;
            }
        }
    }
}
