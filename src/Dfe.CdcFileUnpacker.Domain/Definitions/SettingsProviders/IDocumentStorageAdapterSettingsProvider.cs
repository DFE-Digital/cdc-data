namespace Dfe.CdcFileUnpacker.Domain.Definitions.SettingsProviders
{
    /// <summary>
    /// Describes the operations of the <see cref="IDocumentStorageAdapter" />
    /// settings provider.
    /// </summary>
    public interface IDocumentStorageAdapterSettingsProvider
    {
        /// <summary>
        /// Gets the source stroage connection string.
        /// </summary>
        string SourceStorageConnectionString
        {
            get;
        }

        /// <summary>
        /// Gets the source storage file share name.
        /// </summary>
        string SourceStorageFileShareName
        {
            get;
        }
    }
}