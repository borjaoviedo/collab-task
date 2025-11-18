using Application.Abstractions.Auth;

namespace TestHelpers.Common.Fakes
{
    /// <summary>
    /// Provides a controllable fake implementation of <see cref="ICurrentUserService"/> 
    /// for use in application-layer tests.
    /// </summary>
    public sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? UserId { get; set; }
    }
}
