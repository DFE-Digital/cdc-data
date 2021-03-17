namespace Dfe.CdcFileUnpacker.ConsoleApp.SettingsProviders
{
    using Dfe.CdcFileUnpacker.Application.Definitions.SettingsProvider;

    /// <summary>
    /// Implements <see cref="IUnpackRoutineSettingsProvider" />.
    /// </summary>
    public class UnpackRoutineSettingsProvider : IUnpackRoutineSettingsProvider
    {
        /// <summary>
        /// Initialises a new instance of the
        /// <see cref="UnpackRoutineSettingsProvider" /> class.
        /// </summary>
        /// <param name="degreeOfParallelism">
        /// The number of root directories in which process in parallel.
        /// </param>
        public UnpackRoutineSettingsProvider(byte degreeOfParallelism)
        {
            this.DegreeOfParallelism = degreeOfParallelism;
        }

        /// <summary>
        /// Gets the number of root directories in which process in parallel.
        /// </summary>
        public byte DegreeOfParallelism
        {
            get;
            private set;
        }
    }
}