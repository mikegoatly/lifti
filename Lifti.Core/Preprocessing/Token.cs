using System.Collections.Generic;

namespace Lifti
{
    public class Token
    {
        private readonly List<Range> locations;

        public Token(string token, Range location)
        {
            this.locations = new List<Range> { location };
            this.Value = token;
        }

        public Token(string token, params Range[] locations)
        {
            this.locations = new List<Range>(locations);
            this.Value = token;
        }

        public Token(string token, IReadOnlyList<Range> locations)
        {
            this.locations = new List<Range>(locations);
            this.Value = token;
        }

        public IReadOnlyList<Range> Locations => this.locations;
        public string Value { get; }

        public void AddLocation(Range location)
        {
            this.locations.Add(location);
        }
    }
}
