namespace Dfe.CdcFileUnpacker.Domain.Definitions.SettingsProviders
{
    /// <summary>
    /// Describes the operations of the <see cref="ILoggerWrapper" /> settings
    /// provider.
    /// </summary>
    public interface ILoggerWrapperSettingsProvider
    {
        /// <summary>
        /// Gets a filepath to a directory in which to store the logs.
        /// </summary>
        string LogsDirectory
        {
            get;
        }
    }
}