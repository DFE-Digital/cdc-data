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
        /// Gets or sets a filepath to a directory in which to store the logs.
        /// </summary>
        [Option(
            "logs-directory",
            Default = "logs",
            HelpText = "A filepath to a directory in which to store logs.")]
        public string LogsDirectory
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
    }
}