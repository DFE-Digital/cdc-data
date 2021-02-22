﻿namespace Dfe.CdcFileUnpacker.Application
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

        private const string SitePlanFilenameSuffix = "_application_pdf.zip";

        private const string SitePlanZipFileName = "a";

        private readonly IDocumentStorageAdapter documentStorageAdapter;
        private readonly ILoggerWrapper loggerWrapper;

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
        public UnpackRoutine(
            IDocumentStorageAdapter documentStorageAdapter,
            ILoggerWrapper loggerWrapper)
        {
            this.documentStorageAdapter = documentStorageAdapter;
            this.loggerWrapper = loggerWrapper;
        }

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
            else
            {
                // Other file types. If toReturn == null, then we don't know
                // how to deal with it.
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
            string directory = null;
            foreach (Establishment establishment in establishments)
            {
                // TODO: Update to be parallel. Get it working first.
                directory = establishment.Directory;

                await this.UnpackMigrateFiles(
                    new string[] { rootDirectory, directory },
                    establishment,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task UnpackMigrateFiles(
            string[] directoryPath,
            Establishment establishment,
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

                await this.ProcessFile(documentFile, cancellationToken)
                    .ConfigureAwait(false);

                this.loggerWrapper.Info($"Processed file {documentFile}.");
            }
        }

        private async Task ProcessFile(
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
                            documentFile,
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

        private async Task ProcessSitePlanZip(
            DocumentFile documentFile,
            CancellationToken cancellationToken)
        {
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

            // Then unpack the zip file.
            byte[] downloadedBytesArray = downloadedBytes.ToArray();

            this.loggerWrapper.Debug(
                $"Opening file as a {nameof(ZipArchive)}...");

            byte[] sitePlanBytes = null;
            using (MemoryStream memoryStream = new MemoryStream(downloadedBytesArray))
            {
                using (ZipArchive zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Read))
                {
                    this.loggerWrapper.Debug(
                        $"Opened as {nameof(ZipArchive)}. Extracting file " +
                        $"\"{SitePlanZipFileName}\"...");

                    ZipArchiveEntry zipArchiveEntry = zipArchive.GetEntry(
                        SitePlanZipFileName);

                    using (Stream stream = zipArchiveEntry.Open())
                    {
                        sitePlanBytes = new byte[zipArchiveEntry.Length];

                        await stream.ReadAsync(
                            sitePlanBytes,
                            0,
                            sitePlanBytes.Length)
                            .ConfigureAwait(false);
                    }
                }
            }

            this.loggerWrapper.Info(
                $"\"{SitePlanZipFileName}\" extracted from " +
                $"{nameof(ZipArchive)} ({sitePlanBytes.Length} byte(s)).");
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