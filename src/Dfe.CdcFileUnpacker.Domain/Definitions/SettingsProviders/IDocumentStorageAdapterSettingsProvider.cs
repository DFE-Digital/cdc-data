namespace Dfe.CdcFileUnpacker.Domain.Definitions.SettingsProviders
{
    /// <summary>
    /// Describes the operations of the <see cref="IDocumentStorageAdapter" />
    /// settings provider.
    /// </summary>
    public interface IDocumentStorageAdapterSettingsProvider
    {
        /// <summary>
        /// Gets the destination storage connection string.
        /// </summary>
        string DestinationStorageConnectionString
        {
            get;
        }

        /// <summary>
        /// Gets the destination storage file share name.
        /// </summary>
        string DestinationStorageFileShareName
        {
            get;
        }

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

        /// <summary>
        /// Gets a value indicating whether items in the input csv should be deleted from the target.
        /// </summary>
        bool DeleteFromTarget
        {
            get;
        }
    }
}