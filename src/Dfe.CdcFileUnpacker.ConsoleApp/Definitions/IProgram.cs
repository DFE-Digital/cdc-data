namespace Dfe.CdcFileUnpacker.ConsoleApp.Definitions
{
    using System.Threading;
    using System.Threading.Tasks;
    using Dfe.CdcFileUnpacker.ConsoleApp.Models;

    /// <summary>
    /// Describes the operations of the main entry point class.
    /// </summary>
    public interface IProgram
    {
        /// <summary>
        /// The main, non/static entry method. Where dependency injection
        /// begins.
        /// </summary>
        /// <param name="options">
        /// An instance of <see cref="Options" />.
        /// </param>
        /// <param name="cancellationToken">
        /// An instance of <see cref="CancellationToken" />.
        /// </param>
        /// <returns>
        /// An exit code for the application process.
        /// </returns>
        Task<int> RunAsync(
            Options options,
            CancellationToken cancellationToken);
    }
}