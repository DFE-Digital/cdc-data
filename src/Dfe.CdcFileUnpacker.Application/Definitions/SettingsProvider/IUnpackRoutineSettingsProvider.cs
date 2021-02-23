namespace Dfe.CdcFileUnpacker.Application.Definitions.SettingsProvider
{
    /// <summary>
    /// Describes the operations of the <see cref="IUnpackRoutine" /> settings
    /// provider.
    /// </summary>
    public interface IUnpackRoutineSettingsProvider
    {
        /// <summary>
        /// Gets the number of root directories in which process in parallel.
        /// </summary>
        byte DegreeOfParallelism
        {
            get;
        }
    }
}