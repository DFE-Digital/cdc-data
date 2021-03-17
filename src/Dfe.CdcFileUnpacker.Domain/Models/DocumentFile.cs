namespace Dfe.CdcFileUnpacker.Domain.Models
{
    /// <summary>
    /// Represents a document file.
    /// </summary>
    public class DocumentFile : ModelsBase
    {
        /// <summary>
        /// Gets or sets the absolute path to the file.
        /// </summary>
        public string AbsolutePath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        public string Name
        {
            get;
            set;
        }
    }
}