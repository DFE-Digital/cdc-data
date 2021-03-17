namespace Dfe.CdcFileUnpacker.Domain.Definitions.SettingsProviders
{
    /// <summary>
    /// Describes the operations of the <see cref="IDocumentMetadataAdapter" />
    /// settings provider.
    /// </summary>
    public interface IDocumentMetadataAdapterSettingsProvider
    {
        /// <summary>
        /// Gets the document metatadata connection string.
        /// </summary>
        string DocumentMetadataConnectionString
        {
            get;
        }
    }
}