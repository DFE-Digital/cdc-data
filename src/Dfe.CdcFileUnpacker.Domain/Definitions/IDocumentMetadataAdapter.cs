namespace Dfe.CdcFileUnpacker.Domain.Definitions
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Describes the operations of the document metadata adapter.
    /// </summary>
    public interface IDocumentMetadataAdapter
    {
        /// <summary>
        /// Inserts document metadata.
        /// </summary>
        /// <param name="establishmentId">
        /// The establishment ID/URN.
        /// </param>
        /// <param name="establishmentName">
        /// The establishment name.
        /// </param>
        /// <param name="fileType">
        /// The type of file, a <see cref="FileTypeOption" /> value.
        /// </param>
        /// <param name="fileName">
        /// The name of the file.
        /// </param>
        /// <param name="fileUrl">
        /// The rest of the path to the file.
        /// </param>
        /// <param name="cancellationToken">
        /// An instance of type <see cref="CancellationToken" />.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task CreateDocumentMetadataAsync(
            int establishmentId,
            string establishmentName,
            FileTypeOption fileType,
            string fileName,
            string fileUrl,
            CancellationToken cancellationToken);
    }
}