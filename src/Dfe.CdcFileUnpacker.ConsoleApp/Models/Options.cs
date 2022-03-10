namespace Dfe.CdcFileUnpacker.ConsoleApp.Models
{
    using System.Diagnostics.CodeAnalysis;
    using CommandLine;

    /// <summary>
    /// The options class, as used by the <see cref="CommandLine" />.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Options : ModelsBase
    {
        /// <summary>
        /// Gets or sets the destination storage connection string.
        /// </summary>
        [Option(
            "destination-storage-connection-string",
            Required = true,
            HelpText = "The destination storage connection string.")]
        public string DestinationStorageConnectionString
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the destination storage file share name.
        /// </summary>
        [Option(
            "destination-storage-file-share-name",
            Default = "cdcdocuments",
            HelpText = "The destination storage file share name.")]
        public string DestinationStorageFileShareName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the document metatadata connection string.
        /// </summary>
        [Option(
            "document-metadata-connection-string",
            Required = false,
            HelpText = "The document metadata connection string.")]
        public string DocumentMetadataConnectionString
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the source stroage connection string.
        /// </summary>
        [Option(
            "source-storage-connection-string",
            Required = true,
            HelpText = "The source storage connection string.")]
        public string SourceStorageConnectionString
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the source storage file share name.
        /// </summary>
        [Option(
            "source-storage-file-share-name",
            Default = "accruent",
            HelpText = "The source storage file share name.")]
        public string SourceStorageFileShareName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the number of root directories in which process in
        /// parallel.
        /// </summary>
        [Option(
            "degree-of-parallelism",
            Default = 5,
            HelpText = "The number of root directories in which process in parallel.")]
        public byte DegreeOfParallelism
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether items in the input csv should be deleted from the target.
        /// </summary>
        [Option(
            "delete-from-target",
            Default = false,
            HelpText = "Delete items in csv from target")]
        public bool DeleteFromTarget
        {
            get;
            set;
        }
    }
}