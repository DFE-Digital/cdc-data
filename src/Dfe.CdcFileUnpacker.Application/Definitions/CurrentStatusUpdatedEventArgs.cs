namespace Dfe.CdcFileUnpacker.Application.Definitions
{
    using System;

    /// <summary>
    /// Event arguments for the
    /// <see cref="IUnpackRoutine.CurrentStatusUpdated" /> event.
    /// </summary>
    public class CurrentStatusUpdatedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the current status, as a <see cref="string" />.
        /// </summary>
        public string Message
        {
            get;
            set;
        }
    }
}