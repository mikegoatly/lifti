using System.Collections.Generic;

namespace Lifti.Tokenization
{
    public class Token
    {
        private readonly List<TokenLocation> locations;

        public Token(string token, TokenLocation location)
        {
            this.locations = new List<TokenLocation> { location };
            this.Value = token;
        }

        public Token(string token, params TokenLocation[] locations)
        {
            this.locations = new List<TokenLocation>(locations);
            this.Value = token;
        }

        public Token(string token, IReadOnlyList<TokenLocation> locations)
        {
            this.locations = new List<TokenLocation>(locations);
            this.Value = token;
        }

        public IReadOnlyList<TokenLocation> Locations => this.locations;
        public string Value { get; }

        public void AddLocation(TokenLocation location)
        {
            this.locations.Add(location);
        }
    }
}
