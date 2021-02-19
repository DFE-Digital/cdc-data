namespace Dfe.CdcFileUnpacker.Application
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Dfe.CdcFileUnpacker.Application.Definitions;
    using Dfe.CdcFileUnpacker.Domain.Definitions;

    /// <summary>
    /// Implements <see cref="IUnpackRoutine" />.
    /// </summary>
    public class UnpackRoutine : IUnpackRoutine
    {
        private const string RootCdcDirectory = "CDC";
        private const string RootCdcfeDirectory = "CDCFE";

        private readonly IDocumentStorageAdapter documentStorageAdapter;
        private readonly ILoggerWrapper loggerWrapper;

        /// <summary>
        /// Initialises a new instance of the <see cref="UnpackRoutine" />
        /// class.
        /// </summary>
        /// <param name="documentStorageAdapter">
        /// An instance of type <see cref="IDocumentStorageAdapter" />.
        /// </param>
        /// <param name="loggerWrapper">
        /// An instance of type <see cref="ILoggerWrapper" />.
        /// </param>
        public UnpackRoutine(
            IDocumentStorageAdapter documentStorageAdapter,
            ILoggerWrapper loggerWrapper)
        {
            this.documentStorageAdapter = documentStorageAdapter;
            this.loggerWrapper = loggerWrapper;
        }

        /// <inheritdoc />
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            // 1) Iterate and parse the folder names in the CDC and CDCFE
            //    directories.
            string rootDirectory = RootCdcDirectory;

            this.loggerWrapper.Debug(
                $"Pulling back all directory names in the " +
                $"\"{rootDirectory}\" directory...");

            IEnumerable<string> directoryNames =
                await this.documentStorageAdapter.ListDirectoriesAsync(
                    rootDirectory,
                    cancellationToken)
                .ConfigureAwait(false);

            this.loggerWrapper.Info(
                $"Pulled back {directoryNames.Count()} directory name(s).");
        }
    }
}