namespace Lifti
{
    public class AdvancedOptions
    {
        /// <summary>
        /// Gets or sets the depth of the index tree after which intra-node text is supported.
        /// A value of zero indicates that intra-node text is always supported. To disable
        /// intra-node text completely, set this to an arbitrarily large value, e.g. <see cref="Int32.MaxValue"/>.
        /// </summary>
        public int SupportIntraNodeTextAfterIndexDepth { get; set; } = 4;
    }
}
