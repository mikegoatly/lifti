using System.Collections.Generic;
using System.Collections.Immutable;

namespace Lifti
{
    public class Token
    {
        public Token(string token, Range location)
        {
            this.Locations = ImmutableList<Range>.Empty.Add(location);
            this.Value = token;
        }

        public Token(string token, params Range[] locations)
        {
            this.Locations = locations.ToImmutableList();
            this.Value = token;
        }

        public Token(string token, IReadOnlyList<Range> locations)
        {
            this.Locations = locations.ToImmutableList();
            this.Value = token;
        }

        public ImmutableList<Range> Locations { get; set; }
        public string Value { get; }

        public void AddLocation(Range location)
        {
            this.Locations = this.Locations.Add(location);
        }
    }
}
