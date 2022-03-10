namespace Dfe.CdcFileUnpacker.ConsoleApp.SettingsProviders
{
    using Dfe.CdcFileUnpacker.Domain.Definitions.SettingsProviders;

    /// <summary>
    /// Implements <see cref="IDocumentStorageAdapterSettingsProvider" />.
    /// </summary>
    public class DocumentStorageAdapterSettingsProvider
        : IDocumentStorageAdapterSettingsProvider
    {
        /// <summary>
        /// Initialises a new instance of the
        /// <see cref="DocumentStorageAdapterSettingsProvider" /> class.
        /// </summary>
        /// <param name="destinationStorageConnectionString">
        /// The destination storage connection string.
        /// </param>
        /// <param name="destinationStorageFileShareName">
        /// The destination storage file share name.
        /// </param>
        /// <param name="sourceStorageConnectionString">
        /// The source stroage connection string.
        /// </param>
        /// <param name="sourceStorageFileShareName">
        /// The source storage file share name.
        /// </param>
        /// <param name="deleteFromTarget">
        /// Whether to delete items from the target.
        /// </param>
        public DocumentStorageAdapterSettingsProvider(
            string destinationStorageConnectionString,
            string destinationStorageFileShareName,
            string sourceStorageConnectionString,
            string sourceStorageFileShareName,
            bool deleteFromTarget)
        {
            this.DestinationStorageConnectionString = destinationStorageConnectionString;
            this.DestinationStorageFileShareName = destinationStorageFileShareName;
            this.SourceStorageConnectionString = sourceStorageConnectionString;
            this.SourceStorageFileShareName = sourceStorageFileShareName;
            this.DeleteFromTarget = deleteFromTarget;
        }

        /// <inheritdoc />
        public string DestinationStorageConnectionString
        {
            get;
            private set;
        }

        /// <inheritdoc />
        public string DestinationStorageFileShareName
        {
            get;
            private set;
        }

        /// <inheritdoc />
        public string SourceStorageConnectionString
        {
            get;
            private set;
        }

        /// <inheritdoc />
        public string SourceStorageFileShareName
        {
            get;
            private set;
        }

        /// <inheritdoc />
        public bool DeleteFromTarget
        {
            get;
            private set;
        }
    }
}