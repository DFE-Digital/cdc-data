namespace Dfe.CdcFileUnpacker.Domain.Definitions
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Describes the operations of the logger wrapper.
    /// </summary>
    public interface ILoggerWrapper
    {
        /// <summary>
        /// Logs a <paramref name="message" /> with debug-level importance.
        /// </summary>
        /// <param name="message">
        /// The message to log.
        /// </param>
        /// <param name="exception">
        /// The <see cref="Exception" /> to log. Optional.
        /// </param>
        void Debug(string message, Exception exception = null);

        /// <summary>
        /// Logs a <paramref name="message" /> with info-level importance.
        /// </summary>
        /// <param name="message">
        /// The message to log.
        /// </param>
        /// <param name="exception">
        /// The <see cref="Exception" /> to log. Optional.
        /// </param>
        void Info(string message, Exception exception = null);

        /// <summary>
        /// Logs a <paramref name="message" /> with warning-level importance.
        /// </summary>
        /// <param name="message">
        /// The message to log.
        /// </param>
        /// <param name="exception">
        /// The <see cref="Exception" /> to log. Optional.
        /// </param>
        void Warning(string message, Exception exception = null);

        /// <summary>
        /// Logs a <paramref name="message" /> and an <see cref="Exception" />.
        /// </summary>
        /// <param name="message">
        /// The message to log.
        /// </param>
        /// <param name="exception">
        /// The <see cref="Exception" /> to log. Optional.
        /// </param>
        [SuppressMessage(
            "Microsoft.Naming",
            "CA1716",
            Justification = "Naming logging functions after the level itself is an accepted standard.")]
        void Error(string message, Exception exception = null);
    }
}