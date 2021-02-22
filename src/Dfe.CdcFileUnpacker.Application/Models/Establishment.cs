namespace Dfe.CdcFileUnpacker.Application.Models
{
    /// <summary>
    /// Represents an establishment.
    /// </summary>
    public class Establishment : ModelsBase
    {
        /// <summary>
        /// Gets or sets the original directory.
        /// </summary>
        public string Directory
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the URN.
        /// </summary>
        public long? Urn
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the program.
        /// </summary>
        public string Program
        {
            get;
            set;
        }
    }
}