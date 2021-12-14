namespace Dfe.CdcFileUnpacker.ConsoleApp
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using Dfe.CdcFileUnpacker.ConsoleApp.Models;
    using Dfe.CdcFileUnpacker.Domain.Definitions;
    using Dfe.CdcFileUnpacker.Domain.Definitions.SettingsProviders;

    /// <summary>
    /// Implements <see cref="ILoggerWrapper" />.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class LoggerWrapper : ILoggerWrapper
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="LoggerWrapper" />
        /// class.
        /// </summary>
        public LoggerWrapper()
        {
            // Nothing, for now.
        }

        /// <inheritdoc />
        public void Debug(string message, Exception exception = null)
        {
            WriteConsole(ConsoleColor.White, message, exception);
        }

        /// <inheritdoc />
        public void Error(string message, Exception exception = null)
        {
            WriteConsole(ConsoleColor.Red, message, exception);
        }

        /// <inheritdoc />
        public void Info(string message, Exception exception = null)
        {
            WriteConsole(ConsoleColor.Blue, message, exception);
        }

        /// <inheritdoc />
        public void Warning(string message, Exception exception = null)
        {
            WriteConsole(ConsoleColor.Yellow, message, exception);
        }

        private static void WriteConsole(
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
    }
}