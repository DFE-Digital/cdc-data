namespace Dfe.CdcFileUnpacker.ConsoleApp
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using CsvHelper;
    using Dfe.CdcFileUnpacker.ConsoleApp.Models;
    using Dfe.CdcFileUnpacker.Domain.Definitions.SettingsProviders;
    using Dfe.Spi.Common.Logging.Definitions;

    /// <summary>
    /// Implements <see cref="ILoggerWrapper" />.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class LoggerWrapper : ILoggerWrapper
    {
        private const string LogFilenameDateTimeFormat = "yyyy-MM-dd HH-mm-ss";

        private readonly string logFilePath;

        /// <summary>
        /// Initialises a new instance of the <see cref="LoggerWrapper" />
        /// class.
        /// </summary>
        /// <param name="loggerWrapperSettingsProvider">
        /// An instance of type <see cref="ILoggerWrapperSettingsProvider" />.
        /// </param>
        public LoggerWrapper(
            ILoggerWrapperSettingsProvider loggerWrapperSettingsProvider)
        {
            if (loggerWrapperSettingsProvider == null)
            {
                throw new ArgumentNullException(
                    nameof(loggerWrapperSettingsProvider));
            }

            string logsDirectory = loggerWrapperSettingsProvider.LogsDirectory;

            string logFilename = DateTime.UtcNow.ToString(
                LogFilenameDateTimeFormat,
                CultureInfo.InvariantCulture);

            logFilename = $"{logFilename}.log";

            this.logFilePath = $"{logsDirectory}\\{logFilename}";

            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }

            using (StreamWriter streamWriter = new StreamWriter(this.logFilePath))
            {
                using (CsvWriter csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
                {
                    csvWriter.WriteHeader<LogMessage>();
                    csvWriter.NextRecord();
                }
            }
        }

        /// <inheritdoc />
        public void Debug(string message, Exception exception = null)
        {
            this.WriteConsole(null, message, exception);
            this.WriteFile(nameof(this.Debug), message, exception);
        }

        /// <inheritdoc />
        public void Error(string message, Exception exception = null)
        {
            this.WriteConsole(ConsoleColor.Red, message, exception);
            this.WriteFile(nameof(this.Error), message, exception);
        }

        /// <inheritdoc />
        public void Info(string message, Exception exception = null)
        {
            this.WriteConsole(ConsoleColor.Blue, message, exception);
            this.WriteFile(nameof(this.Info), message, exception);
        }

        /// <inheritdoc />
        public void Warning(string message, Exception exception = null)
        {
            this.WriteConsole(ConsoleColor.Yellow, message, exception);
            this.WriteFile(nameof(this.Warning), message, exception);
        }

        private void WriteConsole(
            ConsoleColor? consoleColor,
            string message,
            Exception exception = null)
        {
            if (consoleColor.HasValue)
            {
                Console.ForegroundColor = consoleColor.Value;
            }
            else
            {
                Console.ResetColor();
            }

            Console.WriteLine(message);

            if (exception != null)
            {
                Console.WriteLine(exception);
            }

            Console.ResetColor();
        }

        private void WriteFile(
            string logLevel,
            string message,
            Exception exception = null)
        {
            using (StreamWriter streamWriter = new StreamWriter(this.logFilePath, true))
            {
                using (CsvWriter csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
                {
                    if (exception != null)
                    {
                        message =
                            $"{message}{Environment.NewLine}{Environment.NewLine}" +
                            $"An exception ({exception.GetType().FullName}) was thrown: {exception.Message}. Stack trace:" +
                            $"{Environment.NewLine}{exception.StackTrace}";
                    }

                    LogMessage logMessage = new LogMessage()
                    {
                        DateTime = DateTime.UtcNow,
                        LogLevel = logLevel,
                        Message = message,
                    };

                    csvWriter.WriteRecord(logMessage);
                    csvWriter.NextRecord();
                }
            }
        }
    }
}