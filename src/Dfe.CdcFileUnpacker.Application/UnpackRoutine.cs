namespace Dfe.CdcFileUnpacker.Application
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Security.Principal;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using Sylvan.Data.Csv;
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
        private const string RootCdcReports = "CDCReports";
        private const string RootCdcReports50 = "CDCReports_50";
        private const string RootCDCReportsExtra = "CDCReports_extra";

        private const string ZipFilenameExtension = ".zip";

        private const string SitePlanFilenameSuffix = "_application_pdf.zip";
        private const string SitePlanOctetFilenameSuffix = "_application_octet-stream.zip";
        private const string EvidenceMimeTypePartialDwg = "dwg";
        private const string EvidenceMimeTypePartialText = "_text_";
        private const string EvidenceMimeTypePartialJpeg = "jpeg";
        private const string EvidenceMimeTypePartialPng = "png";
        private const string ConditionReportArchiveName = "CDC_School_Condition_Report_docx.zip";
        private const string ConditionReportName = "CDC_School_Condition_Report.docx";

        private const string ZipFileItemName = "a";

        private const string DestinationSitePlanSubDirectory = "Site Plan";
        private const string DestinationSitePlanMimeType = "application/pdf";
        private const string DestinationSitePlanFileExtension = ".pdf";

        private const string DestinationEvidenceSubDirectory = "Evidence";
        private const string DestinationEvidenceMimeType = "application/zip";

        private const string DestinationReportSubDirectory = "Condition Report";
        private const string DestinationReportMimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        private const string DestinationReportFileExtension = ".docx";

        private readonly IDocumentMetadataAdapter documentMetadataAdapter;
        private readonly IDocumentStorageAdapter documentStorageAdapter;
        private readonly ILoggerWrapper loggerWrapper;
        private readonly IUnpackRoutineSettingsProvider unpackRoutineSettingsProvider;

        private Dictionary<string, string> _cdc1Evidence = new Dictionary<string, string>();

        /// <summary>
        /// Initialises a new instance of the <see cref="UnpackRoutine" />
        /// class.
        /// </summary>
        /// <param name="documentMetadataAdapter">
        /// An instance of type <see cref="IDocumentMetadataAdapter" />.
        /// </param>
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
            IDocumentMetadataAdapter documentMetadataAdapter,
            IDocumentStorageAdapter documentStorageAdapter,
            ILoggerWrapper loggerWrapper,
            IUnpackRoutineSettingsProvider unpackRoutineSettingsProvider)
        {
            this.documentMetadataAdapter = documentMetadataAdapter;
            this.documentStorageAdapter = documentStorageAdapter;
            this.loggerWrapper = loggerWrapper;
            this.unpackRoutineSettingsProvider = unpackRoutineSettingsProvider;
            this.LoadEvidenceCsv();
        }

        /// <inheritdoc />
        public event EventHandler<CurrentStatusUpdatedEventArgs> CurrentStatusUpdated;

        /// <inheritdoc />
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            // 1) Iterate and parse the folder names in the CDC and CDCFE
            //    directories.
            //    We know these two work, so no need to re-run, for now.
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

            List<string> allUsedReportFilenames = new List<string>();

            List<string> usedReportFilenames = await this.ProcessRootDirectory(
                RootCdcReports,
                false,
                cancellationToken);

            allUsedReportFilenames.AddRange(usedReportFilenames);

            usedReportFilenames = await this.ProcessRootDirectory(
                RootCdcReports50,
                false,
                cancellationToken,
                appendOnlyIfDoesntExist: true,
                usedFilenames: allUsedReportFilenames);

            allUsedReportFilenames.AddRange(usedReportFilenames);

            await this.ProcessRootDirectory(
                RootCDCReportsExtra,
                false,
                cancellationToken,
                appendOnlyIfDoesntExist: true,
                usedFilenames: allUsedReportFilenames);
        }

        private static ZipFileType? GetZipFileType(string zipFileName)
        {
            ZipFileType? toReturn = null;

            // The filename should be in the format
            // GUID_mimetype.zip
            if (zipFileName.EndsWith(SitePlanFilenameSuffix, StringComparison.InvariantCulture) || zipFileName.Contains(EvidenceMimeTypePartialDwg) || zipFileName.EndsWith(SitePlanOctetFilenameSuffix, StringComparison.InvariantCulture))
            {
                toReturn = ZipFileType.SitePlan;
            }
            else if (zipFileName.Contains(EvidenceMimeTypePartialText) || zipFileName.Contains(EvidenceMimeTypePartialJpeg) || zipFileName.Contains(EvidenceMimeTypePartialPng))
            {
                toReturn = ZipFileType.Evidence;
            }
            else if (zipFileName == ConditionReportArchiveName)
            {
                toReturn = ZipFileType.ArchivedReport;
            }
            else if (zipFileName == ConditionReportName)
            {
                toReturn = ZipFileType.Report;
            }
            else
            {
                // We don't know how to deal with this - leave it null.
            }

            return toReturn;
        }

        private static bool ShouldAppend(
            bool appendToDirectoryIfNotExists,
            List<string> usedFileNames,
            string name)
        {
            bool append = true;
            if (appendToDirectoryIfNotExists)
            {
                if (usedFileNames.Exists(x => x.Contains(name)))
                {
                    append = false;
                }
            }

            return append;
        }

        private async Task<List<string>> ProcessRootDirectory(
            string rootDirectory,
            bool filterResults,
            CancellationToken cancellationToken,
            List<string> usedFilenames = null,
            bool appendOnlyIfDoesntExist = false)
        {
            List<string> toReturn = null;

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

            int totalEstablishements = establishments.Count();

            int establishmentCounter = 0;

            byte degreeOfParallelism =
                this.unpackRoutineSettingsProvider.DegreeOfParallelism;

            // We're only going to want to do so many at a time...
            SemaphoreSlim semaphoreSlim = new SemaphoreSlim(
                degreeOfParallelism);

            // This is used by the evidence file naming, and will allow us to
            // track filenames, so we don't get duplicates.
            List<Task<List<string>>> tasks = new List<Task<List<string>>>();

            List<string> usedFilenamesCopy = null;

            Task<List<string>> task = null;
            foreach (Establishment establishment in establishments)
            {
                this.loggerWrapper.Debug(
                    $"Waiting on {nameof(semaphoreSlim)}...");

                await semaphoreSlim.WaitAsync().ConfigureAwait(false);

                this.loggerWrapper.Info(
                    $"{nameof(semaphoreSlim)} unblocked. Starting to " +
                    $"process {establishment}...");

                if (usedFilenames != null)
                {
                    usedFilenamesCopy = usedFilenames.ToList();
                }

                task = Task.Run(
                    () => this.ProcessEstablishment(
                        semaphoreSlim,
                        usedFilenamesCopy, // We want a copy to be passed here, to avoid cross-threading issues.
                        rootDirectory,
                        establishment,
                        appendOnlyIfDoesntExist,
                        cancellationToken));

                establishmentCounter++;

                double currentPercentage = (establishmentCounter / (double)totalEstablishements) * 100;

                this.UpdateCurrentStatus(
                    $"Started processing of establishment " +
                    $"{establishmentCounter}/{totalEstablishements} - " +
                    string.Format(CultureInfo.InvariantCulture, "{0:N2}%", currentPercentage) +
                    $" of root \"{rootDirectory}\"...");

                tasks.Add(task);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            // Combine all the usedFileNames into one big list, and return.
            IEnumerable<string> allUsedFilenames = tasks
                .SelectMany(x => x.Result)
                .Distinct();

            this.loggerWrapper.Info(
                $"All \"{rootDirectory}\" {nameof(Task)}s complete.");

            toReturn = allUsedFilenames.ToList();

            return toReturn;
        }

        [SuppressMessage(
            "Microsoft.Design",
            "CA1031",
            Justification = "Catch-all for anything I've not considered, on an establishment/thread level.")]
        private async Task<List<string>> ProcessEstablishment(
            SemaphoreSlim semaphoreSlim,
            List<string> usedFilenames,
            string rootDirectory,
            Establishment establishment,
            bool appendOnlyIfDoesntExist,
            CancellationToken cancellationToken)
        {
            List<string> toReturn = null;

            try
            {
                if (usedFilenames != null)
                {
                    toReturn = usedFilenames;
                }
                else
                {
                    toReturn = new List<string>();
                }

                string directory = establishment.Directory;

                await this.UnpackMigrateFiles(
                    new string[] { rootDirectory, directory },
                    establishment,
                    toReturn,
                    appendOnlyIfDoesntExist,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                this.loggerWrapper.Error(
                    $"An unhandled exception was thrown while processing " +
                    $"{establishment}. Releasing the " +
                    $"{nameof(semaphoreSlim)} anyway.",
                    exception);

                // TODO: Remove, but leaving in just for now, to figure out if
                //       any exceptions are being thrown, and if I'm not
                //       looking at the screen.
                Environment.Exit(-1);
            }
            finally
            {
                semaphoreSlim.Release();

                this.loggerWrapper.Info(
                    $"Finished processing {establishment}. " +
                    $"{nameof(semaphoreSlim)} released.");
            }

            return toReturn;
        }

        private async Task UnpackMigrateFiles(
            string[] directoryPath,
            Establishment establishment,
            List<string> usedFileNames,
            bool appendOnlyIfDoesntExist,
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
                    appendOnlyIfDoesntExist,
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
                    appendOnlyIfDoesntExist,
                    cancellationToken)
                    .ConfigureAwait(false);

                this.loggerWrapper.Info($"Processed file {documentFile}.");
            }
        }

        private async Task ProcessFile(
            Establishment establishment,
            List<string> usedFileNames,
            DocumentFile documentFile,
            bool appendToDirectoryIfNotExists,
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
                            usedFileNames,
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

                    case ZipFileType.ArchivedReport:
                        await this.ProcessArchivedReport(
                            establishment,
                            documentFile,
                            usedFileNames,
                            appendToDirectoryIfNotExists,
                            cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ZipFileType.Report:
                        await this.ProcessReport(
                            establishment,
                            documentFile,
                            usedFileNames,
                            appendToDirectoryIfNotExists,
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

        private string GenerateUniqueName(
            string name,
            string seperator,
            string filenameExtension,
            List<string> usedFileNames)
        {
            string toReturn = null;

            int indexOfFileExt = name.IndexOf(
                filenameExtension,
                StringComparison.InvariantCulture);

            if (indexOfFileExt > 0)
            {
                string nameFormat = name.Insert(indexOfFileExt, seperator + "{0}");

                int i = 1;
                do
                {
                    toReturn = string.Format(
                        CultureInfo.InvariantCulture,
                        nameFormat,
                        i);

                    this.loggerWrapper.Debug(
                        $"{nameof(toReturn)} = \"{toReturn}\"");

                    i++;
                }
                while (usedFileNames.Contains(toReturn));

                this.loggerWrapper.Info(
                    $"Destination filename finalised: \"{toReturn}\". " +
                    $"Adding to {nameof(usedFileNames)}.");

                usedFileNames.Add(toReturn);
            }
            else
            {
                this.loggerWrapper.Warning(
                    $"This file \"{name}\" does not appear to be a zip " +
                    $"file! It will be ignored!");
            }

            return toReturn;
        }

        private async Task ProcessEvidence(
            Establishment establishment,
            DocumentFile documentFile,
            List<string> usedFileNames,
            CancellationToken cancellationToken)
        {
            byte[] unzippedByteArray = await this.DownloadAndUnzip(
                documentFile,
                cancellationToken)
                .ConfigureAwait(false);

            // Attempt to get filename from evidence data
            string evidenceName = string.Empty;
            string idSegment = this.GetIdFromName(documentFile.Name);
            if (idSegment != string.Empty)
            {
                var keyExists = this._cdc1Evidence.TryGetValue(idSegment.ToLower(), out evidenceName);
                if (keyExists)
                {
                    this.loggerWrapper.Info($"Evidence name: {evidenceName}");
                } else {
                    this.loggerWrapper.Info($"Entry not found for id {idSegment}, skipping file");
                    return;
                }

                evidenceName += this.GetFileExtension(documentFile.Name);
                evidenceName = this.StripIllegalCharacters(evidenceName);
            }

            await this.SendFileToDestinationStorage(
                establishment,
                DestinationEvidenceSubDirectory,
                evidenceName,
                DestinationEvidenceMimeType,
                unzippedByteArray,
                cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<byte[]> DownloadAndUnzip(
            DocumentFile documentFile,
            CancellationToken cancellationToken)
        {
            byte[] toReturn = null;

            byte[] downloadedBytesArray = await this.DownloadDocument(
                documentFile,
                cancellationToken)
                .ConfigureAwait(false);

            // Then unpack the zip file.
            this.loggerWrapper.Debug(
                $"Opening file as a {nameof(ZipArchive)}...");

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

                            toReturn = destinationMemoryStream.ToArray();
                        }
                    }
                }
            }

            this.loggerWrapper.Info(
                $"\"{ZipFileItemName}\" extracted from " +
                $"{nameof(ZipArchive)} ({toReturn.Length} byte(s)).");

            return toReturn;
        }

        private async Task ProcessReport(
            Establishment establishment,
            DocumentFile documentFile,
            List<string> usedFileNames,
            bool appendToDirectoryIfNotExists,
            CancellationToken cancellationToken)
        {
            string name = establishment.Name;

            bool append = ShouldAppend(
                appendToDirectoryIfNotExists,
                usedFileNames,
                name);

            if (append)
            {
                string evidenceName = name + DestinationReportFileExtension;

                byte[] reportBytes = await this.DownloadDocument(
                    documentFile,
                    cancellationToken)
                    .ConfigureAwait(false);

                Uri uri = await this.SendFileToDestinationStorage(
                    establishment,
                    DestinationReportSubDirectory,
                    evidenceName,
                    DestinationReportMimeType,
                    reportBytes,
                    cancellationToken)
                    .ConfigureAwait(false);

                FileTypeOption fileType = FileTypeOption.Report;
                await this.InsertMetaData(
                    establishment,
                    fileType,
                    uri,
                    name,
                    evidenceName,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                this.loggerWrapper.Warning(
                    $"A file already exists with a name similar to " +
                    $"\"{name}\", and " +
                    $"{nameof(appendToDirectoryIfNotExists)} = false. " +
                    $"Therefore, this file will be ignored.");
            }
        }

        private async Task ProcessArchivedReport(
            Establishment establishment,
            DocumentFile documentFile,
            List<string> usedFileNames,
            bool appendToDirectoryIfNotExists,
            CancellationToken cancellationToken)
        {
            string name = establishment.Name;

            bool append = ShouldAppend(
                appendToDirectoryIfNotExists,
                usedFileNames,
                name);

            if (append)
            {
                string evidenceName = name + DestinationReportFileExtension;

                byte[] reportBytes = await this.DownloadAndUnzip(
                    documentFile,
                    cancellationToken)
                    .ConfigureAwait(false);

                Uri uri = await this.SendFileToDestinationStorage(
                    establishment,
                    DestinationReportSubDirectory,
                    evidenceName,
                    DestinationReportMimeType,
                    reportBytes,
                    cancellationToken)
                    .ConfigureAwait(false);

                FileTypeOption fileType = FileTypeOption.Report;
                await this.InsertMetaData(
                    establishment,
                    fileType,
                    uri,
                    name,
                    evidenceName,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                this.loggerWrapper.Warning(
                    $"A file already exists with a name similar to " +
                    $"\"{name}\", and " +
                    $"{nameof(appendToDirectoryIfNotExists)} = false. " +
                    $"Therefore, this file will be ignored.");
            }
        }

        private async Task ProcessSitePlanZip(
            Establishment establishment,
            DocumentFile documentFile,
            List<string> usedFileNames,
            CancellationToken cancellationToken)
        {
            byte[] sitePlanBytes = await this.DownloadAndUnzip(
                documentFile,
                cancellationToken)
                .ConfigureAwait(false);

            if (sitePlanBytes.Length < 10000)
            {
                // probably a text file, skip it
                this.loggerWrapper.Info($"{documentFile.Name} with mime type octet-stream is less than 10kb, skipping");
                return;
            }

            string name = establishment.Name;
            string filename = name + DestinationSitePlanFileExtension;

            // Attempt to get filename from evidence data
            string idSegment = this.GetIdFromName(documentFile.Name);
            string evidenceName = string.Empty;
            if (idSegment != string.Empty)
            {
                var keyExists = this._cdc1Evidence.TryGetValue(idSegment.ToLower(), out evidenceName);
                if (keyExists)
                {
                    this.loggerWrapper.Info($"Evidence name: {evidenceName}");
                }
                else
                {
                    this.loggerWrapper.Info($"Entry not found for id {idSegment}, skipping file");
                    return;
                }

                evidenceName += this.GetFileExtension(documentFile.Name);
                evidenceName = this.StripIllegalCharacters(evidenceName);
            }

            Uri uri = await this.SendFileToDestinationStorage(
                establishment,
                DestinationSitePlanSubDirectory,
                evidenceName,
                DestinationSitePlanMimeType,
                sitePlanBytes,
                cancellationToken)
                .ConfigureAwait(false);

            FileTypeOption fileType = FileTypeOption.SitePlan;
            await this.InsertMetaData(
                establishment,
                fileType,
                uri,
                name,
                evidenceName,
                cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task InsertMetaData(
            Establishment establishment,
            FileTypeOption fileType,
            Uri uri,
            string name,
            string filename,
            CancellationToken cancellationToken)
        {
            // Then insert the metadata.
            string[] segments = uri.Segments;
            string fileName = segments.Last();
            string fileNameDecoded = HttpUtility.UrlDecode(filename);
            string fileUrl = uri.AbsoluteUri.Replace(fileName, string.Empty);

            await this.documentMetadataAdapter.CreateDocumentMetadataAsync(
                establishment.Urn.Value,
                name,
                fileType,
                fileNameDecoded,
                fileUrl,
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

        private async Task<Uri> SendFileToDestinationStorage(
            Establishment establishment,
            string destinationSubDirectory,
            string filename,
            string destinationMimeType,
            byte[] bytes,
            CancellationToken cancellationToken)
        {
            Uri toReturn = null;

            if (bytes.Length < 1)
            {
                this.loggerWrapper.Info($"Skipping file {filename} as it is zero length");
                return null;
            }

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

            toReturn = await this.documentStorageAdapter.UploadFileAsync(
                new string[] { destinationEstablishmentDir, destinationSubDirectory, },
                filename,
                destinationMimeType,
                bytes,
                cancellationToken)
                .ConfigureAwait(false);

            this.loggerWrapper.Info(
                $"File \"{filename}\" sent to destination storage.");

            return toReturn;
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

            int? urn = null;

            try
            {
                urn = int.Parse(urnStr, CultureInfo.InvariantCulture);
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

        private string GetIdFromName(string name)
        {
            int indexOfSeparator = name.IndexOf(
                "_",
                StringComparison.InvariantCulture);

            if (indexOfSeparator > 0)
            {
                return name.Substring(0, indexOfSeparator);
            }
            else
            {
                return string.Empty;
            }
        }

        private string GetFileExtension(string name)
        {
            if (name.Contains(EvidenceMimeTypePartialJpeg)) { return ".jpeg"; }
            if (name.Contains(EvidenceMimeTypePartialPng)) { return ".png"; }
            if (name.Contains(EvidenceMimeTypePartialText)) { return ".txt"; }
            if (name.EndsWith(SitePlanFilenameSuffix, StringComparison.InvariantCulture)) { return ".pdf"; }
            if (name.EndsWith(SitePlanOctetFilenameSuffix, StringComparison.InvariantCulture)) { return ".dwg"; }
            if (name.Contains(EvidenceMimeTypePartialDwg)) { return ".dwg"; }
            if (name == ConditionReportArchiveName || name == ConditionReportName) { return ".docx"; }
            return string.Empty;
        }

        private void LoadEvidenceCsv()
        {
            this.loggerWrapper.Info("Reading CDC1 evidence data...");
            using (var csv = CsvDataReader.Create("cdc1-evidence.csv"))
            {
                while (csv.Read())
                {
                    var exists = this._cdc1Evidence.ContainsKey(csv.GetString(0));
                    if (!exists)
                    {
                        this._cdc1Evidence.Add(csv.GetString(0).ToLower(), csv.GetString(1));
                    }
                    else
                    {
                        this.loggerWrapper.Warning($"Row with key {csv.GetString(0)}, value {csv.GetString(1)} could not be added, key already exists");
                    }
                }
            }
            this.loggerWrapper.Info($"CDC1 evidence data loaded ({this._cdc1Evidence.Count} rows)");
        }

        private string StripIllegalCharacters(string fileName)
        {
            fileName = fileName.Replace("/", string.Empty);
            fileName = fileName.Replace(@"\", string.Empty);
            fileName = fileName.Replace(":", string.Empty);
            fileName = fileName.Replace("|", string.Empty);
            fileName = fileName.Replace("<", string.Empty);
            fileName = fileName.Replace(">", string.Empty);
            fileName = fileName.Replace("*", string.Empty);
            fileName = fileName.Replace("?", string.Empty);

            return fileName;
        }
    }
}