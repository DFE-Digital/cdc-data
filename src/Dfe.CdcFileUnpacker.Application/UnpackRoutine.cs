namespace Dfe.CdcFileUnpacker.Application
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Dfe.CdcFileUnpacker.Application.Definitions;
    using Dfe.CdcFileUnpacker.Application.Definitions.SettingsProvider;
    using Dfe.CdcFileUnpacker.Application.Models;
    using Dfe.CdcFileUnpacker.Domain.Definitions;
    using Dfe.CdcFileUnpacker.Domain.Models;

    /// <summary>
    /// Implements <see cref="IUnpackRoutine" />.
    /// </summary>
    public class UnpackRoutine : IUnpackRoutine
    {
        private const string RootCdcDirectory = "CDC";
        private const string RootCdcfeDirectory = "CDCFE";

        private const string ZipFilenameExtension = ".zip";

        private const string SitePlanFilenameSuffix = "_application_pdf.zip";
        private const string EvidenceMimeTypePartialText = "_text_";
        private const string EvidenceMimeTypePartialImage = "_image_";

        private const string ZipFileItemName = "a";

        private const string DestinationSitePlanSubDirectory = "Site Plan";
        private const string DestinationSitePlanMimeType = "application/pdf";
        private const string DestinationSitePlanFileExtension = ".pdf";

        private const string DestinationEvidenceSubDirectory = "Evidence";
        private const string DestinationEvidenceMimeType = "application/zip";

        private readonly IDocumentStorageAdapter documentStorageAdapter;
        private readonly ILoggerWrapper loggerWrapper;
        private readonly IUnpackRoutineSettingsProvider unpackRoutineSettingsProvider;

        private int establishmentCounter;

        /// <summary>
        /// Initialises a new instance of the <see cref="UnpackRoutine" />
        /// class.
        /// </summary>
        /// <param name="documentStorageAdapter">
        /// An instance of type <see cref="IDocumentStorageAdapter" />.
        /// </param>
        /// <param name="loggerWrapper">
        /// An instance of type <see cref="ILoggerWrapper" />.
        /// </param>
        /// <param name="unpackRoutineSettingsProvider">
        /// An instance of type <see cref="IUnpackRoutineSettingsProvider" />.
        /// </param>
        public UnpackRoutine(
            IDocumentStorageAdapter documentStorageAdapter,
            ILoggerWrapper loggerWrapper,
            IUnpackRoutineSettingsProvider unpackRoutineSettingsProvider)
        {
            this.documentStorageAdapter = documentStorageAdapter;
            this.loggerWrapper = loggerWrapper;
            this.unpackRoutineSettingsProvider = unpackRoutineSettingsProvider;
        }

        /// <inheritdoc />
        public event EventHandler<CurrentStatusUpdatedEventArgs> CurrentStatusUpdated;

        /// <inheritdoc />
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            // 1) Iterate and parse the folder names in the CDC and CDCFE
            //    directories.
            await this.ProcessRootDirectory(
                RootCdcDirectory,
                true,
                cancellationToken)
                .ConfigureAwait(false);

            await this.ProcessRootDirectory(
                RootCdcfeDirectory,
                false,
                cancellationToken)
                .ConfigureAwait(false);
        }

        private static ZipFileType? GetZipFileType(string zipFileName)
        {
            ZipFileType? toReturn = null;

            // The filename should be in the format
            // GUID_mimetype.zip
            if (zipFileName.EndsWith(SitePlanFilenameSuffix, StringComparison.InvariantCulture))
            {
                toReturn = ZipFileType.SitePlan;
            }
            else if (zipFileName.Contains(EvidenceMimeTypePartialText) || zipFileName.Contains(EvidenceMimeTypePartialImage))
            {
                toReturn = ZipFileType.Evidence;
            }
            else
            {
                // We don't know how to deal with this - leave it null.
            }

            return toReturn;
        }

        private async Task ProcessRootDirectory(
            string rootDirectory,
            bool filterResults,
            CancellationToken cancellationToken)
        {
            // First, parse the establishment folders, and filter down to the
            // directories we want to scan.
            this.UpdateCurrentStatus(
                $"Initialising unpack routine (\"{rootDirectory}\")...");

            this.loggerWrapper.Debug(
                $"Pulling back list of all {nameof(Establishment)}(s) to " +
                $"process...");

            IEnumerable<Establishment> establishments =
                await this.GetEstablishmentsAsync(
                    rootDirectory,
                    filterResults,
                    cancellationToken)
                .ConfigureAwait(false);

            this.loggerWrapper.Info(
                $"{establishments.Count()} {nameof(Establishment)}(s) " +
                $"parsed.");

            // Second, process each establishment in turn.
            int totalEstablishements = establishments.Count();

            this.establishmentCounter = 0;

            byte degreeOfParallelism =
                this.unpackRoutineSettingsProvider.DegreeOfParallelism;

            // We're only going to want to do so many at a time...
            SemaphoreSlim semaphoreSlim = new SemaphoreSlim(
                degreeOfParallelism);

            // This is used by the evidence file naming, and will allow us to
            // track filenames, so we don't get duplicates.
            List<Task> tasks = new List<Task>();

            Task task = null;
            foreach (Establishment establishment in establishments)
            {
                this.loggerWrapper.Debug(
                    $"Waiting on {nameof(semaphoreSlim)}...");

                await semaphoreSlim.WaitAsync().ConfigureAwait(false);

                this.loggerWrapper.Info(
                    $"{nameof(semaphoreSlim)} unblocked. Starting to " +
                    $"process {establishment}...");

                task = Task.Run(() =>
                    this.ProcessEstablishment(
                        semaphoreSlim,
                        rootDirectory,
                        totalEstablishements,
                        establishment,
                        cancellationToken));

                tasks.Add(task);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            this.loggerWrapper.Info(
                $"All \"{rootDirectory}\" {nameof(Task)}s complete.");
        }

        private async Task ProcessEstablishment(
            SemaphoreSlim semaphoreSlim,
            string rootDirectory,
            int totalEstablishments,
            Establishment establishment,
            CancellationToken cancellationToken)
        {
            double currentPercentage = (this.establishmentCounter / (double)totalEstablishments) * 100;

            string directory = establishment.Directory;

            this.UpdateCurrentStatus(
                $"Processing \"{directory}\" - " +
                string.Format(CultureInfo.InvariantCulture, "{0:N2}%", currentPercentage) +
                $" of root \"{rootDirectory}\"...");

            List<string> usedFileNames = new List<string>();

            await this.UnpackMigrateFiles(
                new string[] { rootDirectory, directory },
                establishment,
                usedFileNames,
                cancellationToken)
                .ConfigureAwait(false);

            this.establishmentCounter++;

            semaphoreSlim.Release();

            this.loggerWrapper.Info(
                $"Finished processing {establishment}. " +
                $"{nameof(semaphoreSlim)} released.");
        }

        private async Task UnpackMigrateFiles(
            string[] directoryPath,
            Establishment establishment,
            List<string> usedFileNames,
            CancellationToken cancellationToken)
        {
            string topLevelDirectory = directoryPath.Last();

            // First, delve down recursively.
            this.loggerWrapper.Debug(
                $"Pulling inner directories within \"{topLevelDirectory}\"...");

            IEnumerable<string> innerDirectories =
                await this.documentStorageAdapter.ListDirectoriesAsync(
                    directoryPath,
                    cancellationToken)
                .ConfigureAwait(false);

            this.loggerWrapper.Info(
                $"{innerDirectories.Count()} inner directory(s) returned.");

            this.loggerWrapper.Debug("Looping through inner directories...");

            string[] innerDirectoryPath = null;
            foreach (string innerDirectory in innerDirectories)
            {
                innerDirectoryPath = directoryPath
                    .Concat(new string[] { innerDirectory })
                    .ToArray();

                this.loggerWrapper.Debug(
                    $"Processing inner directory \"{innerDirectory}\"...");

                await this.UnpackMigrateFiles(
                    innerDirectoryPath,
                    establishment,
                    usedFileNames,
                    cancellationToken)
                    .ConfigureAwait(false);

                this.loggerWrapper.Info(
                    $"Inner directory \"{innerDirectory}\" processed.");
            }

            this.loggerWrapper.Debug(
                $"Now scanning for files in \"{topLevelDirectory}\"...");

            IEnumerable<DocumentFile> documentFiles =
                await this.documentStorageAdapter.ListFilesAsync(
                    directoryPath,
                    cancellationToken)
                .ConfigureAwait(false);

            this.loggerWrapper.Info(
                $"{documentFiles.Count()} file(s) returned.");

            foreach (DocumentFile documentFile in documentFiles)
            {
                this.loggerWrapper.Debug(
                    $"Processing file {documentFile}...");

                await this.ProcessFile(
                    establishment,
                    usedFileNames,
                    documentFile,
                    cancellationToken)
                    .ConfigureAwait(false);

                this.loggerWrapper.Info($"Processed file {documentFile}.");
            }
        }

        private async Task ProcessFile(
            Establishment establishment,
            List<string> usedFileNames,
            DocumentFile documentFile,
            CancellationToken cancellationToken)
        {
            this.loggerWrapper.Debug(
                $"Determining the {nameof(ZipFileType)} for " +
                $"{documentFile}...");

            string name = documentFile.Name;

            ZipFileType? zipFileType = GetZipFileType(name);
            if (zipFileType.HasValue)
            {
                this.loggerWrapper.Info(
                    $"{nameof(zipFileType)} = {zipFileType.Value}");

                switch (zipFileType.Value)
                {
                    case ZipFileType.SitePlan:
                        await this.ProcessSitePlanZip(
                            establishment,
                            documentFile,
                            cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ZipFileType.Evidence:
                        await this.ProcessEvidence(
                            establishment,
                            documentFile,
                            usedFileNames,
                            cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    default:
                        this.loggerWrapper.Error(
                            "Able to determine the zip file type, but the " +
                            "processing functionality has not been " +
                            "implemented yet.");
                        break;
                }
            }
            else
            {
                this.loggerWrapper.Warning(
                    $"Could not determine {nameof(ZipFileType)} for " +
                    $"\"{name}\". It will be ignored.");
            }
        }

        private async Task ProcessEvidence(
            Establishment establishment,
            DocumentFile documentFile,
            List<string> usedFileNames,
            CancellationToken cancellationToken)
        {
            byte[] downloadedBytesArray = await this.DownloadDocument(
                documentFile,
                cancellationToken)
                .ConfigureAwait(false);

            // The filename should be the same as the original, just ammended
            // slightly.
            string name = documentFile.Name;

            int indexOfFileExt = name.IndexOf(
                ZipFilenameExtension,
                StringComparison.InvariantCulture);

            if (indexOfFileExt > 0)
            {
                string nameFormat = name.Insert(indexOfFileExt, "_{0}");

                int i = 1;
                string candidateName = null;

                do
                {
                    candidateName = string.Format(
                        CultureInfo.InvariantCulture,
                        nameFormat,
                        i);

                    this.loggerWrapper.Debug(
                        $"{nameof(candidateName)} = \"{candidateName}\"");

                    i++;
                }
                while (usedFileNames.Contains(candidateName));

                this.loggerWrapper.Info(
                    $"Destination filename finalised: \"{candidateName}\". " +
                    $"Adding to {nameof(usedFileNames)}.");

                usedFileNames.Add(candidateName);

                await this.SendFileToDestinationStorage(
                    establishment,
                    DestinationEvidenceSubDirectory,
                    candidateName,
                    DestinationEvidenceMimeType,
                    downloadedBytesArray,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                this.loggerWrapper.Warning(
                    $"This file \"{name}\" does not appear to be a zip " +
                    $"file! It will be ignored!");
            }
        }

        private async Task ProcessSitePlanZip(
            Establishment establishment,
            DocumentFile documentFile,
            CancellationToken cancellationToken)
        {
            byte[] downloadedBytesArray = await this.DownloadDocument(
                documentFile,
                cancellationToken)
                .ConfigureAwait(false);

            // Then unpack the zip file.
            this.loggerWrapper.Debug(
                $"Opening file as a {nameof(ZipArchive)}...");

            byte[] sitePlanBytes = null;
            using (MemoryStream memoryStream = new MemoryStream(downloadedBytesArray))
            {
                using (ZipArchive zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Read))
                {
                    this.loggerWrapper.Debug(
                        $"Opened as {nameof(ZipArchive)}. Extracting file " +
                        $"\"{ZipFileItemName}\"...");

                    ZipArchiveEntry zipArchiveEntry = zipArchive.GetEntry(
                        ZipFileItemName);

                    using (Stream stream = zipArchiveEntry.Open())
                    {
                        using (MemoryStream destinationMemoryStream = new MemoryStream())
                        {
                            await stream.CopyToAsync(destinationMemoryStream)
                                .ConfigureAwait(false);

                            sitePlanBytes = destinationMemoryStream.ToArray();
                        }
                    }
                }
            }

            this.loggerWrapper.Info(
                $"\"{ZipFileItemName}\" extracted from " +
                $"{nameof(ZipArchive)} ({sitePlanBytes.Length} byte(s)).");

            string name = establishment.Name;
            string filename = name + DestinationSitePlanFileExtension;

            await this.SendFileToDestinationStorage(
                establishment,
                DestinationSitePlanSubDirectory,
                filename,
                DestinationSitePlanMimeType,
                sitePlanBytes,
                cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<byte[]> DownloadDocument(
            DocumentFile documentFile,
            CancellationToken cancellationToken)
        {
            byte[] toReturn = null;

            this.loggerWrapper.Debug($"Downloading {documentFile}...");

            string absolutePath = documentFile.AbsolutePath;
            IEnumerable<byte> downloadedBytes =
                await this.documentStorageAdapter.DownloadFileAsync(
                    absolutePath,
                    cancellationToken)
                .ConfigureAwait(false);

            this.loggerWrapper.Info(
                $"Downloaded {documentFile} ({downloadedBytes.Count()} " +
                $"byte(s)).");

            toReturn = downloadedBytes.ToArray();

            return toReturn;
        }

        private async Task SendFileToDestinationStorage(
            Establishment establishment,
            string destinationSubDirectory,
            string filename,
            string destinationMimeType,
            byte[] bytes,
            CancellationToken cancellationToken)
        {
            // Construct a directory for the establishment in the destination
            // storage.
            // Should have a URN, as I believe we're filtering this out before.
            long urn = establishment.Urn.Value;
            string name = establishment.Name;
            string program = establishment.Program;

            string destinationEstablishmentDir =
                string.Format(CultureInfo.InvariantCulture, "{0:00000}", urn) +
                $" {name} ({program})";

            this.loggerWrapper.Debug(
                $"{nameof(destinationEstablishmentDir)} = " +
                $"\"{destinationEstablishmentDir}\"");

            this.loggerWrapper.Debug(
                $"Sending file \"{filename}\" to destination storage...");

            await this.documentStorageAdapter.UploadFileAsync(
                new string[] { destinationEstablishmentDir, destinationSubDirectory, },
                filename,
                destinationMimeType,
                bytes,
                cancellationToken)
                .ConfigureAwait(false);

            this.loggerWrapper.Info(
                $"File \"{filename}\" sent to destination storage.");
        }

        private async Task<IEnumerable<Establishment>> GetEstablishmentsAsync(
            string rootDirectory,
            bool filterResults,
            CancellationToken cancellationToken)
        {
            List<Establishment> toReturn = null;

            this.loggerWrapper.Debug(
                $"Pulling back all directory names in the " +
                $"\"{rootDirectory}\" directory...");

            IEnumerable<string> directoryNames =
                await this.documentStorageAdapter.ListDirectoriesAsync(
                    new string[] { rootDirectory },
                    cancellationToken)
                .ConfigureAwait(false);

            this.loggerWrapper.Info(
                    $"Pulled back {directoryNames.Count()} directory " +
                    $"name(s).");

            this.loggerWrapper.Debug(
                $"Converting directory names to {nameof(Establishment)} " +
                $"instances...");

            toReturn = directoryNames
                .Select(this.ConvertEstablishmentStringToModel)
                .ToList();

            this.loggerWrapper.Info(
                $"{toReturn.Count} {nameof(Establishment)}s " +
                $"obtained.");

            this.loggerWrapper.Debug(
                $"Filtering out establishements with the " +
                $"\"{rootDirectory}\" program and with a URN...");

            if (filterResults)
            {
                toReturn = toReturn
                    .Where(x => x.Program == rootDirectory)
                    .ToList();
            }

            toReturn = toReturn
                .Where(x => x.Urn.HasValue)
                .ToList();

            this.loggerWrapper.Info(
                $"Filtered {toReturn.Count} result(s).");

            return toReturn;
        }

        private void UpdateCurrentStatus(string message)
        {
            if (this.CurrentStatusUpdated != null)
            {
                CurrentStatusUpdatedEventArgs currentStatusUpdatedEventArgs =
                    new CurrentStatusUpdatedEventArgs()
                    {
                        Message = message,
                    };

                this.CurrentStatusUpdated(this, currentStatusUpdatedEventArgs);
            }
        }

        private Establishment ConvertEstablishmentStringToModel(
            string establishmentStr)
        {
            Establishment toReturn = null;

            string[] parts = establishmentStr.Split(' ');

            string urnStr = parts.First();

            long? urn = null;

            try
            {
                urn = long.Parse(urnStr, CultureInfo.InvariantCulture);
            }
            catch (FormatException formatException)
            {
                this.loggerWrapper.Warning(
                    $"Could not parse out the {urn} for " +
                    $"\"{establishmentStr}\"!",
                    formatException);
            }

            string programParens = parts.Last();

            string program = null;
            if (programParens.Contains('(') && programParens.Contains(')'))
            {
                program = programParens
                    .Replace("(", string.Empty)
                    .Replace(")", string.Empty);
            }

            string name = establishmentStr;

            if (urn.HasValue)
            {
                name = name.Replace(urnStr, string.Empty).Trim();
            }

            name = name
                .Replace(programParens, string.Empty)
                .Trim();

            if (name.StartsWith("- ", StringComparison.InvariantCulture))
            {
                name = name.Remove(0, 2).Trim();
            }

            toReturn = new Establishment()
            {
                Directory = establishmentStr,
                Urn = urn,
                Name = name,
                Program = program,
            };

            return toReturn;
        }
    }
}