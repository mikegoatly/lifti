using System.Collections.Generic;

namespace Lifti.Tokenization
{
    public class Token
    {
        private readonly List<WordLocation> locations;

        public Token(string token, WordLocation location)
        {
            this.locations = new List<WordLocation> { location };
            this.Value = token;
        }

        public Token(string token, params WordLocation[] locations)
        {
            this.locations = new List<WordLocation>(locations);
            this.Value = token;
        }

        public Token(string token, IReadOnlyList<WordLocation> locations)
        {
            this.locations = new List<WordLocation>(locations);
            this.Value = token;
        }

        public IReadOnlyList<WordLocation> Locations => this.locations;
        public string Value { get; }

        public void AddLocation(WordLocation location)
        {
            this.locations.Add(location);
        }
    }
}
