namespace Dfe.CdcFileUnpacker.Infrastructure.SqlServer
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Threading;
    using System.Threading.Tasks;
    using Dapper;
    using Dfe.CdcFileUnpacker.Domain.Definitions;
    using Dfe.CdcFileUnpacker.Domain.Definitions.SettingsProviders;

    /// <summary>
    /// Implements <see cref="IDocumentMetadataAdapter" />.
    /// </summary>
    public class DocumentMetadataAdapter : IDocumentMetadataAdapter
    {
        private const string DefaultSupplierKeyIDValue = "abc";
        private const string DefaultSiteVisitDate = "2020-01-01";

        private readonly IDocumentMetadataAdapterSettingsProvider documentMetadataAdapterSettingsProvider;
        private readonly ILoggerWrapper loggerWrapper;

        /// <summary>
        /// Initialises a new instance of the
        /// <see cref="DocumentMetadataAdapter" /> class.
        /// </summary>
        /// <param name="documentMetadataAdapterSettingsProvider">
        /// An instance of type
        /// <see cref="IDocumentMetadataAdapterSettingsProvider" />.
        /// </param>
        /// <param name="loggerWrapper">
        /// An instance of type <see cref="ILoggerWrapper" />.
        /// </param>
        public DocumentMetadataAdapter(
            IDocumentMetadataAdapterSettingsProvider documentMetadataAdapterSettingsProvider,
            ILoggerWrapper loggerWrapper)
        {
            this.documentMetadataAdapterSettingsProvider = documentMetadataAdapterSettingsProvider;
            this.loggerWrapper = loggerWrapper;
        }

        /// <inheritdoc />
        public async Task CreateDocumentMetadataAsync(
            int establishmentId,
            string establishmentName,
            FileTypeOption fileType,
            string fileName,
            string fileUrl,
            CancellationToken cancellationToken)
        {
            byte fileTypeId = (byte)fileType;

            DateTime siteVisitDate = DateTime.Parse(DefaultSiteVisitDate);

            var parameters = new
            {
                EstablishmentID = establishmentId,
                SupplierKeyID = DefaultSupplierKeyIDValue,
                EstablishmentName = establishmentName,
                FileTypeID = fileTypeId,
                SiteVisitDate = siteVisitDate,
                FileName = fileName,
                FileURL = fileUrl,
            };

            using (SqlConnection sqlConnection = await this.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                await sqlConnection.ExecuteAsync(
                    "sp_INSERT_FileData",
                    parameters,
                    commandType: CommandType.StoredProcedure)
                    .ConfigureAwait(false);
            }
        }

        private async Task<SqlConnection> GetOpenConnectionAsync(
            CancellationToken cancellationToken)
        {
            SqlConnection toReturn = null;

            string fileMetaDataConnectionString =
                this.documentMetadataAdapterSettingsProvider.DocumentMetadataConnectionString;

            toReturn = new SqlConnection(fileMetaDataConnectionString);

            this.loggerWrapper.Debug(
                $"Opening new {nameof(SqlConnection)} using " +
                $"{nameof(fileMetaDataConnectionString)}...");

            await toReturn.OpenAsync(cancellationToken).ConfigureAwait(false);

            return toReturn;
        }
    }
}