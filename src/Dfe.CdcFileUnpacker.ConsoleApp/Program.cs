namespace Dfe.CdcFileUnpacker.ConsoleApp
{
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using CommandLine;
    using Dfe.CdcFileUnpacker.ConsoleApp.Definitions;
    using Dfe.CdcFileUnpacker.ConsoleApp.Models;
    using Dfe.CdcFileUnpacker.ConsoleApp.SettingsProviders;
    using Dfe.CdcFileUnpacker.Domain.Definitions.SettingsProviders;
    using Dfe.Spi.Common.Logging.Definitions;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Implements <see cref="IProgram" />.
    /// </summary>
    public class Program : IProgram
    {
        private readonly ILoggerWrapper loggerWrapper;

        /// <summary>
        /// Initialises a new instance of the <see cref="Program" /> class.
        /// </summary>
        /// <param name="loggerWrapper">
        /// An instance of type <see cref="ILoggerWrapper" />.
        /// </param>
        public Program(ILoggerWrapper loggerWrapper)
        {
            this.loggerWrapper = loggerWrapper;
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

            this.loggerWrapper.Debug("This is a test debug message.");
            this.loggerWrapper.Info("This is a test info message.");
            this.loggerWrapper.Warning("Test warning message.");
            this.loggerWrapper.Error("Test error.");

            try
            {
                throw new FileLoadException("blurgh");
            }
            catch (System.Exception exception)
            {
                this.loggerWrapper.Error("Whoops.", exception);
            }

            toReturn = 0;

            return toReturn;
        }

        [ExcludeFromCodeCoverage]
        private static async Task<int> InvokeRunAsnyc(
            Options options,
            CancellationToken cancellationToken)
        {
            int toReturn = -1;

            string logsDirectory = options.LogsDirectory;

            LoggerWrapperSettingsProvider loggerWrapperSettingsProvider =
                new LoggerWrapperSettingsProvider(logsDirectory);

            using (ServiceProvider serviceProvider = CreateServiceProvider(loggerWrapperSettingsProvider))
            {
                IProgram program = serviceProvider.GetService<IProgram>();

                toReturn = await program.RunAsync(options, cancellationToken)
                    .ConfigureAwait(false);
            }

            return toReturn;
        }

        [ExcludeFromCodeCoverage]
        private static ServiceProvider CreateServiceProvider(
            LoggerWrapperSettingsProvider loggerWrapperSettingsProvider)
        {
            ServiceProvider toReturn = null;

            IServiceCollection serviceCollection = new ServiceCollection()
                .AddSingleton<ILoggerWrapperSettingsProvider>(loggerWrapperSettingsProvider)
                .AddSingleton<ILoggerWrapper, LoggerWrapper>()
                .AddScoped<IProgram, Program>();

            toReturn = serviceCollection.BuildServiceProvider();

            return toReturn;
        }
    }
}