namespace Dfe.CdcFileUnpacker.ConsoleApp.SettingsProviders
{
    using Dfe.CdcFileUnpacker.Domain.Definitions.SettingsProviders;

    /// <summary>
    /// Implements <see cref="ILoggerWrapperSettingsProvider" />.
    /// </summary>
    public class LoggerWrapperSettingsProvider : ILoggerWrapperSettingsProvider
    {
        /// <summary>
        /// Initialises a new instance of the
        /// <see cref="LoggerWrapperSettingsProvider" /> class.
        /// </summary>
        /// <param name="logsDirectory">
        /// A filepath to a directory in which to store the logs.
        /// </param>
        public LoggerWrapperSettingsProvider(string logsDirectory)
        {
            this.LogsDirectory = logsDirectory;
        }

        /// <inheritdoc />
        public string LogsDirectory
        {
            get;
            private set;
        }
    }
}