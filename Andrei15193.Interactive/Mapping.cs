namespace Andrei15193.Interactive
{
    /// <summary>
    /// Represents a mapping from a source instance to a destination instance.
    /// </summary>
    public class Mapping
    {
        /// <summary>
        /// Gets or sets the source instance of the mapping.
        /// </summary>
        public object From { get; set; }

        /// <summary>
        /// Gets or sets the destintion instance of the mapping.
        /// </summary>
        public object To { get; set; }
    }
}