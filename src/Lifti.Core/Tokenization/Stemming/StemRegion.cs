namespace Lifti.Tokenization.Stemming
{
    /// <summary>
    /// Information about the index of the R1 and R2 regions within the word being stemmed.
    /// </summary>
    internal readonly struct StemRegion
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StemRegion"/> struct.
        /// </summary>
        /// <param name="r1">The index of the region after the first non-vowel following a vowel, 
        /// or the index after the end of the word if there is no such non-vowel.</param>
        /// <param name="r2">The index of the region after the first non-vowel following a vowel in R1, 
        /// or the index after the end of the word if there is no such non-vowel.</param>
        public StemRegion(int r1, int r2)
        {
            this.R1 = r1;
            this.R2 = r2;
        }

        /// <summary>
        /// Gets the index of the region after the first non-vowel following a vowel, 
        /// or the index after the end of the word if there is no such non-vowel.
        /// </summary>
        /// <value>The R1 index.</value>
        public int R1 { get; }

        /// <summary>
        /// Gets the index of the region after the first non-vowel following a vowel in R1, 
        /// or the index after the end of the word if there is no such non-vowel.
        /// </summary>
        /// <value>The R2 index.</value>
        public int R2 { get; }
    }
}
