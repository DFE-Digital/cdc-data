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
        /// <param name="sourceStorageConnectionString">
        /// The source stroage connection string.
        /// </param>
        /// <param name="sourceStorageFileShareName">
        /// The source storage file share name.
        /// </param>
        public DocumentStorageAdapterSettingsProvider(
            string sourceStorageConnectionString,
            string sourceStorageFileShareName)
        {
            this.SourceStorageConnectionString = sourceStorageConnectionString;
            this.SourceStorageFileShareName = sourceStorageFileShareName;
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
    }
}