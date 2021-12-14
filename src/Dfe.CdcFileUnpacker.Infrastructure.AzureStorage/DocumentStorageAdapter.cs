namespace Dfe.CdcFileUnpacker.Infrastructure.AzureStorage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Mime;
    using System.Threading;
    using System.Threading.Tasks;
    using Dfe.CdcFileUnpacker.Domain.Definitions;
    using Dfe.CdcFileUnpacker.Domain.Definitions.SettingsProviders;
    using Dfe.CdcFileUnpacker.Domain.Models;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.File;

    /// <summary>
    /// Implements <see cref="IDocumentStorageAdapter" />.
    /// </summary>
    public class DocumentStorageAdapter : IDocumentStorageAdapter
    {
        private readonly ILoggerWrapper loggerWrapper;

        private readonly CloudFileShare destinationCloudFileShare;
        private readonly CloudFileShare sourceCloudFileShare;
        private readonly FileRequestOptions fileRequestOptions;
        private readonly OperationContext operationContext;
        private readonly StorageCredentials sourceStorageCredentials;

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

            CloudStorageAccount sourceCloudStorageAccount =
                CloudStorageAccount.Parse(sourceStorageConnectionString);

            this.sourceStorageCredentials =
                sourceCloudStorageAccount.Credentials;

            CloudFileClient sourceCloudFileClient =
                sourceCloudStorageAccount.CreateCloudFileClient();

            string sourceStorageFileShareName =
                documentStorageAdapterSettingsProvider.SourceStorageFileShareName;

            this.sourceCloudFileShare =
                sourceCloudFileClient.GetShareReference(
                    sourceStorageFileShareName);

            string destinationStorageConnectionString =
                documentStorageAdapterSettingsProvider.DestinationStorageConnectionString;

            CloudStorageAccount destinationCloudStorageAccount =
                CloudStorageAccount.Parse(destinationStorageConnectionString);

            CloudFileClient destinationCloudFileClient =
                destinationCloudStorageAccount.CreateCloudFileClient();

            string destinationStorageFileShareName =
                documentStorageAdapterSettingsProvider.DestinationStorageFileShareName;

            this.destinationCloudFileShare =
                destinationCloudFileClient.GetShareReference(
                    destinationStorageFileShareName);

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
        public async Task<IEnumerable<byte>> DownloadFileAsync(
            string absolutePath,
            CancellationToken cancellationToken)
        {
            byte[] toReturn = null;

            Uri uri = new Uri(absolutePath, UriKind.Absolute);
            CloudFile cloudFile = new CloudFile(uri, this.sourceStorageCredentials);

            await cloudFile.FetchAttributesAsync().ConfigureAwait(false);

            toReturn = new byte[cloudFile.Properties.Length];

            this.loggerWrapper.Debug(
                $"Downloading \"{absolutePath}\" ({toReturn.Length} " +
                $"byte(s))...");

            await cloudFile.DownloadToByteArrayAsync(toReturn, 0)
                .ConfigureAwait(false);

            this.loggerWrapper.Info(
                $"Downloaded \"{absolutePath}\" ({toReturn.Length} byte(s)).");

            return toReturn;
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
        public async Task<IEnumerable<DocumentFile>> ListFilesAsync(
            string[] directoryPath,
            CancellationToken cancellationToken)
        {
            IEnumerable<DocumentFile> toReturn = null;

            IEnumerable<CloudFile> cloudFileDirectories =
                await this.ListFileItems<CloudFile>(
                    directoryPath,
                    cancellationToken)
                .ConfigureAwait(false);

            this.loggerWrapper.Info(
                $"Listing of {nameof(CloudFile)}s complete. " +
                $"Returning names...");

            toReturn = cloudFileDirectories.Select(x =>
                new DocumentFile()
                {
                    AbsolutePath = x.Uri.ToString(),
                    Name = x.Name,
                });

            return toReturn;
        }

        /// <inheritdoc />
        public async Task<Uri> UploadFileAsync(
            string[] directoryPath,
            string filename,
            string mimeType,
            IEnumerable<byte> bytes,
            CancellationToken cancellationToken)
        {
            Uri toReturn = null;

            if (directoryPath == null)
            {
                throw new ArgumentNullException(nameof(directoryPath));
            }

            this.loggerWrapper.Debug(
                $"Getting root directory reference for " +
                $"\"{this.sourceCloudFileShare.Name}\"...");

            CloudFileDirectory shareRoot =
                this.destinationCloudFileShare.GetRootDirectoryReference();

            CloudFileDirectory innerDir = shareRoot.GetDirectoryReference("0_CDC1");
            bool dirCreated;
            foreach (string directory in directoryPath)
            {
                this.loggerWrapper.Debug(
                    $"Getting reference to directory \"{directory}\"...");

                innerDir = innerDir.GetDirectoryReference(directory);

                dirCreated = await innerDir.CreateIfNotExistsAsync(
                    this.fileRequestOptions,
                    this.operationContext,
                    cancellationToken)
                    .ConfigureAwait(false);

                if (dirCreated)
                {
                    this.loggerWrapper.Info(
                        $"\"{directory}\" did not exist. It has been " +
                        $"created.");
                }
                else
                {
                    this.loggerWrapper.Debug(
                        $"\"{directory}\" exists already.");
                }
            }

            CloudFile cloudFile = innerDir.GetFileReference(filename);

            this.loggerWrapper.Debug(
                $"Uploading {bytes.Count()} {nameof(Byte)}s with filename " +
                $"\"{filename}\"...");

            byte[] buffer = bytes.ToArray();

            await cloudFile.UploadFromByteArrayAsync(
                buffer,
                0,
                buffer.Length,
                AccessCondition.GenerateEmptyCondition(),
                this.fileRequestOptions,
                this.operationContext,
                cancellationToken)
                .ConfigureAwait(false);

            this.loggerWrapper.Info(
                $"{bytes.Count()} {nameof(Byte)}s uploaded, with filename " +
                $"\"{filename}\".");

            toReturn = cloudFile.Uri;

            this.loggerWrapper.Debug(
                $"Updating content type (\"{mimeType}\") on file...");

            cloudFile.Properties.ContentType = mimeType;

            await cloudFile.SetPropertiesAsync(
                AccessCondition.GenerateEmptyCondition(),
                this.fileRequestOptions,
                this.operationContext,
                cancellationToken)
                .ConfigureAwait(false);

            this.loggerWrapper.Info(
                $"Content type updated to \"{mimeType}\".");

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
                $"\"{this.sourceCloudFileShare.Name}\"...");

            CloudFileDirectory shareRoot =
                this.sourceCloudFileShare.GetRootDirectoryReference();

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