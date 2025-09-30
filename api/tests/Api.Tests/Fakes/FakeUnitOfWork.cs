using Application.Common.Abstractions.Persistence;

namespace Api.Tests.Fakes
{
    public sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
