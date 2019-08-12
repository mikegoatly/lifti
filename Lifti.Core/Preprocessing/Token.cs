using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Lifti
{
    public class Token
    {
        public Token(ReadOnlySpan<char> token, Range location)
        {
            this.Locations = ImmutableList<Range>.Empty.Add(location);
            this.Token = token.ToArray();
        }

        public Token(ReadOnlySpan<char> token, params Range[] locations)
        {
            this.Locations = locations.ToImmutableList();
            this.Token = token.ToArray();
        }

        public Token(ReadOnlySpan<char> token, IReadOnlyList<Range> locations)
        {
            this.Locations = locations.ToImmutableList();
            this.Token = token.ToArray();
        }

        public ImmutableList<Range> Locations { get; set; }
        public char[] Token { get; }

        public void AddLocation(Range location)
        {
            this.Locations = this.Locations.Add(location);
        }
    }
}
