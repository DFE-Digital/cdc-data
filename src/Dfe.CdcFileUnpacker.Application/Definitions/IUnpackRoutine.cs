namespace Dfe.CdcFileUnpacker.Application.Definitions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Describes the operations of the unpack routine.
    /// </summary>
    public interface IUnpackRoutine
    {
        /// <summary>
        /// Event is raised when the current status is updated.
        /// </summary>
        event EventHandler<CurrentStatusUpdatedEventArgs> CurrentStatusUpdated;

        /// <summary>
        /// Runs the unpack routine.
        /// </summary>
        /// <param name="cancellationToken">
        /// An instance of <see cref="CancellationToken" />.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task RunAsync(CancellationToken cancellationToken);
    }
}