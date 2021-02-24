namespace Dfe.CdcFileUnpacker.ConsoleApp
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using CommandLine;
    using Dfe.CdcFileUnpacker.Application;
    using Dfe.CdcFileUnpacker.Application.Definitions;
    using Dfe.CdcFileUnpacker.Application.Definitions.SettingsProvider;
    using Dfe.CdcFileUnpacker.ConsoleApp.Definitions;
    using Dfe.CdcFileUnpacker.ConsoleApp.Models;
    using Dfe.CdcFileUnpacker.ConsoleApp.SettingsProviders;
    using Dfe.CdcFileUnpacker.Domain.Definitions;
    using Dfe.CdcFileUnpacker.Domain.Definitions.SettingsProviders;
    using Dfe.CdcFileUnpacker.Infrastructure.AzureStorage;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Implements <see cref="IProgram" />.
    /// </summary>
    public class Program : IProgram
    {
        private const string ConsoleTitleFormat = "[CdcFileUnpacker] {0}";

        private readonly ILoggerWrapper loggerWrapper;
        private readonly IUnpackRoutine unpackRoutine;

        /// <summary>
        /// Initialises a new instance of the <see cref="Program" /> class.
        /// </summary>
        /// <param name="loggerWrapper">
        /// An instance of type <see cref="ILoggerWrapper" />.
        /// </param>
        /// <param name="unpackRoutine">
        /// An instance of type <see cref="IUnpackRoutine" />.
        /// </param>
        public Program(
            ILoggerWrapper loggerWrapper,
            IUnpackRoutine unpackRoutine)
        {
            this.loggerWrapper = loggerWrapper;
            this.unpackRoutine = unpackRoutine;

            this.unpackRoutine.CurrentStatusUpdated += this.UnpackRoutine_CurrentStatusUpdated;
        }

        /// <summary>
        /// Main entry method for the console app.
        /// </summary>
        /// <param name="args">
        /// The command line arguments.
        /// </param>
        /// <returns>
        /// An exit code for the application process.
        /// </returns>
        [ExcludeFromCodeCoverage]
        public static async Task<int> Main(
            string[] args)
        {
            int toReturn = -1;

            await Parser.Default
                .ParseArguments<Options>(args)
                .WithParsedAsync(async x =>
                {
                    toReturn = await InvokeRunAsnyc(x, CancellationToken.None)
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);

            return toReturn;
        }

        /// <inheritdoc />
        public async Task<int> RunAsync(
            Options options,
            CancellationToken cancellationToken)
        {
            int toReturn = -1;

            try
            {
                this.loggerWrapper.Debug(
                    $"Starting the {nameof(IUnpackRoutine)}...");

                await this.unpackRoutine.RunAsync(cancellationToken)
                    .ConfigureAwait(false);

                this.loggerWrapper.Info(
                    $"{nameof(IUnpackRoutine)} completed with success.");

                toReturn = 0;
            }
            catch (Exception exception)
            {
                this.loggerWrapper.Error(
                    $"An unhandled exception was thrown while running the " +
                    $"{nameof(IUnpackRoutine)}. Exception details are " +
                    $"included.",
                    exception);
            }

            return toReturn;
        }

        [ExcludeFromCodeCoverage]
        private static async Task<int> InvokeRunAsnyc(
            Options options,
            CancellationToken cancellationToken)
        {
            int toReturn = -1;

            string destinationStorageConnectionString =
                options.DestinationStorageConnectionString;
            string destinationStorageFileShareName =
                options.DestinationStorageFileShareName;
            string sourceStorageConnectionString =
                options.SourceStorageConnectionString;
            string sourceStorageFileShareName =
                options.SourceStorageFileShareName;
            DocumentStorageAdapterSettingsProvider documentStorageAdapterSettingsProvider =
                new DocumentStorageAdapterSettingsProvider(
                    destinationStorageConnectionString,
                    destinationStorageFileShareName,
                    sourceStorageConnectionString,
                    sourceStorageFileShareName);

            byte degreeOfParallelism = options.DegreeOfParallelism;
            UnpackRoutineSettingsProvider unpackRoutineSettingsProvider =
                new UnpackRoutineSettingsProvider(degreeOfParallelism);

            using (ServiceProvider serviceProvider = CreateServiceProvider(documentStorageAdapterSettingsProvider, unpackRoutineSettingsProvider))
            {
                IProgram program = serviceProvider.GetService<IProgram>();

                toReturn = await program.RunAsync(options, cancellationToken)
                    .ConfigureAwait(false);
            }

            return toReturn;
        }

        [ExcludeFromCodeCoverage]
        private static ServiceProvider CreateServiceProvider(
            DocumentStorageAdapterSettingsProvider documentStorageAdapterSettingsProvider,
            UnpackRoutineSettingsProvider unpackRoutineSettingsProvider)
        {
            ServiceProvider toReturn = null;

            IServiceCollection serviceCollection = new ServiceCollection()
                .AddSingleton<IDocumentStorageAdapterSettingsProvider>(documentStorageAdapterSettingsProvider)
                .AddSingleton<IUnpackRoutineSettingsProvider>(unpackRoutineSettingsProvider)
                .AddScoped<IDocumentStorageAdapter, DocumentStorageAdapter>()
                .AddSingleton<ILoggerWrapper, LoggerWrapper>()
                .AddSingleton<IUnpackRoutine, UnpackRoutine>()
                .AddSingleton<IProgram, Program>();

            toReturn = serviceCollection.BuildServiceProvider();

            return toReturn;
        }

        private void UnpackRoutine_CurrentStatusUpdated(
            object sender,
            CurrentStatusUpdatedEventArgs e)
        {
            string message = e.Message;

            message = string.Format(
                CultureInfo.InvariantCulture,
                ConsoleTitleFormat,
                message);

            Console.Title = message;
        }
    }
}