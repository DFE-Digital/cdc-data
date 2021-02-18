namespace Dfe.CdcFileUnpacker.ConsoleApp.Models
{
    using System;

    /// <summary>
    /// Model used by <see cref="CsvHelper" /> to represent a log message.
    /// </summary>
    public class LogMessage : ModelsBase
    {
        /// <summary>
        /// Gets or sets the <see cref="System.DateTime" /> of the message.
        /// </summary>
        public DateTime DateTime
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the log level of the message.
        /// </summary>
        public string LogLevel
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        public string Message
        {
            get;
            set;
        }
    }
}