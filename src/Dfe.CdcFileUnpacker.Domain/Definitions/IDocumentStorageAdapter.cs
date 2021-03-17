namespace Dfe.CdcFileUnpacker.Domain.Definitions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Dfe.CdcFileUnpacker.Domain.Models;

    /// <summary>
    /// Describes the operations of the document storage adapter.
    /// </summary>
    public interface IDocumentStorageAdapter
    {
        /// <summary>
        /// Downloads a file for a given <paramref name="absolutePath" />.
        /// </summary>
        /// <param name="absolutePath">
        /// An absolute path to the file.
        /// </param>
        /// <param name="cancellationToken">
        /// An instance of type <see cref="CancellationToken" />.
        /// </param>
        /// <returns>
        /// An instance of type <see cref="IEnumerable{Byte}" />.
        /// </returns>
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

        /// <summary>
        /// Updates a file to the <paramref name="directoryPath" />.
        /// </summary>
        /// <param name="directoryPath">
        /// An array/path to a directory.
        /// </param>
        /// <param name="filename">
        /// The name of the file to upload as.
        /// </param>
        /// <param name="mimeType">
        /// The mimetype of the file to upload.
        /// </param>
        /// <param name="bytes">
        /// The content of the file as an instance of
        /// <see cref="IEnumerable{Byte}" />.
        /// </param>
        /// <param name="cancellationToken">
        /// An instance of <see cref="CancellationToken" />.
        /// </param>
        /// <returns>
        /// A <see cref="Uri" /> to the created file.
        /// </returns>
        Task<Uri> UploadFileAsync(
            string[] directoryPath,
            string filename,
            string mimeType,
            IEnumerable<byte> bytes,
            CancellationToken cancellationToken);
    }
}