namespace Dfe.CdcFileUnpacker.Application.Models
{
    /// <summary>
    /// Describes values that differentiate different types of ZIP file, so
    /// that they can be processed accordingly.
    /// </summary>
    public enum ZipFileType
    {
        /// <summary>
        /// Represents a site plan.
        /// </summary>
        SitePlan,

        /// <summary>
        /// Represents evidence.
        /// </summary>
        Evidence,
    }
}