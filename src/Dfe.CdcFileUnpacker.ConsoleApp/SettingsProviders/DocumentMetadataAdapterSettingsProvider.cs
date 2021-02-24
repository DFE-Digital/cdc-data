namespace Dfe.CdcFileUnpacker.ConsoleApp.SettingsProviders
{
    using Dfe.CdcFileUnpacker.Domain.Definitions.SettingsProviders;

    /// <summary>
    /// Implements <see cref="IDocumentMetadataAdapterSettingsProvider" />.
    /// </summary>
    public class DocumentMetadataAdapterSettingsProvider
        : IDocumentMetadataAdapterSettingsProvider
    {
        /// <summary>
        /// Initialises a new instance of the
        /// <see cref="DocumentMetadataAdapterSettingsProvider" /> class.
        /// </summary>
        /// <param name="documentMetadataConnectionString">
        /// The document metatadata connection string.
        /// </param>
        public DocumentMetadataAdapterSettingsProvider(
            string documentMetadataConnectionString)
        {
            this.DocumentMetadataConnectionString = documentMetadataConnectionString;
        }

        /// <inheritdoc />
        public string DocumentMetadataConnectionString
        {
            get;
            private set;
        }
    }
}