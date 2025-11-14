using Domain.Enums;

namespace Application.Abstractions.Extensions
{
    /// <summary>
    /// Provides extension methods for converting <see cref="PrecheckStatus"/>
    /// values into <see cref="DomainMutation"/> results.
    /// </summary>
    public static class PrecheckStatusExtensions
    {
        /// <summary>
        /// Maps a <see cref="PrecheckStatus"/> to its corresponding <see cref="DomainMutation"/> error state.
        /// </summary>
        /// <param name="status">The precheck status returned by a repository operation.</param>
        /// <returns>
        /// The corresponding <see cref="DomainMutation"/> for <see cref="PrecheckStatus.NotFound"/>,
        /// <see cref="PrecheckStatus.NoOp"/>, or <see cref="PrecheckStatus.Conflict"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown when the status value is not expected to map to an error.</exception>
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
