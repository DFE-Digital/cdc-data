namespace Dfe.CdcFileUnpacker.Infrastructure.AzureStorage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Dfe.CdcFileUnpacker.Domain.Definitions;
    using Dfe.CdcFileUnpacker.Domain.Definitions.SettingsProviders;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.File;

    /// <summary>
    /// Implements <see cref="IDocumentStorageAdapter" />.
    /// </summary>
    public class DocumentStorageAdapter : IDocumentStorageAdapter
    {
        private readonly ILoggerWrapper loggerWrapper;

        private readonly CloudFileShare cloudFileShare;
        private readonly FileRequestOptions fileRequestOptions;
        private readonly OperationContext operationContext;

        /// <summary>
        /// Initialises a new instance of the
        /// <see cref="DocumentStorageAdapter" /> class.
        /// </summary>
        /// <param name="documentStorageAdapterSettingsProvider">
        /// An instance of type
        /// <see cref="IDocumentStorageAdapterSettingsProvider" />.
        /// </param>
        /// <param name="loggerWrapper">
        /// An instance of type <see cref="ILoggerWrapper" />.
        /// </param>
        public DocumentStorageAdapter(
            IDocumentStorageAdapterSettingsProvider documentStorageAdapterSettingsProvider,
            ILoggerWrapper loggerWrapper)
        {
            if (documentStorageAdapterSettingsProvider == null)
            {
                throw new ArgumentNullException(
                    nameof(documentStorageAdapterSettingsProvider));
            }

            string sourceStorageConnectionString =
                documentStorageAdapterSettingsProvider.SourceStorageConnectionString;

            CloudStorageAccount cloudStorageAccount =
                CloudStorageAccount.Parse(sourceStorageConnectionString);

            CloudFileClient cloudFileClient =
                cloudStorageAccount.CreateCloudFileClient();

            string sourceStorageFileShareName =
                documentStorageAdapterSettingsProvider.SourceStorageFileShareName;

            this.cloudFileShare = cloudFileClient.GetShareReference(
                sourceStorageFileShareName);

            this.loggerWrapper = loggerWrapper;

            this.fileRequestOptions = new FileRequestOptions()
            {
                // Just default, for now.
            };

            this.operationContext = new OperationContext()
            {
                // Just default, for now.
            };
        }

        /// <inheritdoc />
        public async Task<IEnumerable<string>> ListDirectoriesAsync(
            string[] directoryPath,
            CancellationToken cancellationToken)
        {
            IEnumerable<string> toReturn = null;

            IEnumerable<CloudFileDirectory> cloudFileDirectories =
                await this.ListFileItems<CloudFileDirectory>(
                    directoryPath,
                    cancellationToken)
                .ConfigureAwait(false);

            this.loggerWrapper.Info(
                $"Listing of {nameof(CloudFileDirectory)}s complete. " +
                $"Returning names...");

            toReturn = cloudFileDirectories.Select(x => x.Name);

            return toReturn;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<string>> ListFilesAsync(
            string[] directoryPath,
            CancellationToken cancellationToken)
        {
            IEnumerable<string> toReturn = null;

            IEnumerable<CloudFile> cloudFileDirectories =
                await this.ListFileItems<CloudFile>(
                    directoryPath,
                    cancellationToken)
                .ConfigureAwait(false);

            this.loggerWrapper.Info(
                $"Listing of {nameof(CloudFile)}s complete. " +
                $"Returning names...");

            toReturn = cloudFileDirectories.Select(x => x.Name);

            return toReturn;
        }

        private async Task<IEnumerable<TListFileItem>> ListFileItems<TListFileItem>(
            string[] directoryPath,
            CancellationToken cancellationToken)
            where TListFileItem : IListFileItem
        {
            IEnumerable<TListFileItem> toReturn = null;

            if (directoryPath == null)
            {
                throw new ArgumentNullException(nameof(directoryPath));
            }

            this.loggerWrapper.Debug(
                $"Getting root directory reference for " +
                $"\"{this.cloudFileShare.Name}\"...");

            CloudFileDirectory shareRoot =
                this.cloudFileShare.GetRootDirectoryReference();

            CloudFileDirectory innerDir = shareRoot;
            foreach (string directory in directoryPath)
            {
                this.loggerWrapper.Debug(
                    $"Getting reference to directory \"{directory}\"...");

                innerDir = innerDir.GetDirectoryReference(directory);
            }

            this.loggerWrapper.Debug(
                "Beginning listing of files/directories...");

            List<TListFileItem> listFileItems = new List<TListFileItem>();

            IEnumerable<IListFileItem> results = null;
            IEnumerable<TListFileItem> castedResults = null;
            FileContinuationToken fileContinuationToken = null;
            do
            {
                FileResultSegment fileResultSegment =
                    await innerDir.ListFilesAndDirectoriesSegmentedAsync(
                        null,
                        fileContinuationToken,
                        this.fileRequestOptions,
                        this.operationContext,
                        cancellationToken)
                    .ConfigureAwait(false);

                results = fileResultSegment.Results;

                this.loggerWrapper.Debug(
                    $"{results.Count()} result(s) returned. Filtering to " +
                    $"directories...");

                castedResults = results
                    .Where(x => x is TListFileItem)
                    .Cast<TListFileItem>();

                this.loggerWrapper.Debug(
                    $"Adding {results.Count()} filtered results to the " +
                    $"overall results list...");

                listFileItems.AddRange(castedResults);

                this.loggerWrapper.Info(
                    $"Total filtered results so far: " +
                    $"{listFileItems.Count}.");

                fileContinuationToken = fileResultSegment.ContinuationToken;

                if (fileContinuationToken != null)
                {
                    this.loggerWrapper.Debug(
                        $"{nameof(FileContinuationToken)} present. Looping " +
                        $"round again...");
                }
            }
            while (fileContinuationToken != null);

            toReturn = listFileItems.ToList();

            return toReturn;
        }
    }
}