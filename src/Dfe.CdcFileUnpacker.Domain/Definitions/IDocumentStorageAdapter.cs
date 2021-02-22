namespace Dfe.CdcFileUnpacker.Domain.Definitions
{
    using Dfe.CdcFileUnpacker.Domain.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Describes the operations of the document storage adapter.
    /// </summary>
    public interface IDocumentStorageAdapter
    {
        Task<IEnumerable<byte>> DownloadFileAsync(
            string absolutePath,
            CancellationToken cancellationToken);

        /// <summary>
        /// Lists the names of directories for a given
        /// <paramref name="directoryPath" />.
        /// </summary>
        /// <param name="directoryPath">
        /// An array/path to a directory.
        /// </param>
        /// <param name="cancellationToken">
        /// An instance of type <see cref="CancellationToken" />.
        /// </param>
        /// <returns>
        /// An instance of type <see cref="IEnumerable{String}" />.
        /// </returns>
        Task<IEnumerable<string>> ListDirectoriesAsync(
            string[] directoryPath,
            CancellationToken cancellationToken);

        /// <summary>
        /// Lists the names of files for a given
        /// <paramref name="directoryPath" />.
        /// </summary>
        /// <param name="directoryPath">
        /// An array/path to a directory.
        /// </param>
        /// <param name="cancellationToken">
        /// An instance of type <see cref="CancellationToken" />.
        /// </param>
        /// <returns>
        /// An instance of type <see cref="IEnumerable{DocumentFile}" />.
        /// </returns>
        Task<IEnumerable<DocumentFile>> ListFilesAsync(
            string[] directoryPath,
            CancellationToken cancellationToken);
    }
}