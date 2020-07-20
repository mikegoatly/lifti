using System.Collections.Generic;

namespace Lifti.Tokenization
{
    /// <summary>
    /// Provides information about a token extracted from a document.
    /// </summary>
    public class Token
    {
        private readonly List<TokenLocation> locations;

        /// <summary>
        /// Initializes a new instance of the <see cref="Token"/> class.
        /// </summary>
        public Token(string token, TokenLocation location)
        {
            this.locations = new List<TokenLocation> { location };
            this.Value = token;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Token"/> class.
        /// </summary>
        public Token(string token, params TokenLocation[] locations)
        {
            this.locations = new List<TokenLocation>(locations);
            this.Value = token;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Token"/> class.
        /// </summary>
        public Token(string token, IReadOnlyList<TokenLocation> locations)
        {
            this.locations = new List<TokenLocation>(locations);
            this.Value = token;
        }

        /// <summary>
        /// Gets the locations at which the token was located in the document.
        /// </summary>
        public IReadOnlyList<TokenLocation> Locations => this.locations;

        /// <summary>
        /// Gets the value of the token in its normalized form.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Adds a location to this instance.
        /// </summary>
        public void AddLocation(TokenLocation location)
        {
            this.locations.Add(location);
        }
    }
}
