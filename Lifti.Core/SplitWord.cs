using System;
using System.Collections.Immutable;

namespace Lifti
{
    public class SplitWord
    {
        public SplitWord(ReadOnlySpan<char> word, Range location)
        {
            this.Locations = ImmutableList<Range>.Empty.Add(location);
            this.Word = word.ToArray();
        }

        public SplitWord(ReadOnlySpan<char> word, params Range[] locations)
        {
            this.Locations = ImmutableList<Range>.Empty.AddRange(locations);
            this.Word = word.ToArray();
        }

        public ImmutableList<Range> Locations { get; set; }
        public char[] Word { get; }

        public void AddLocation(Range location)
        {
            this.Locations = this.Locations.Add(location);
        }
    }
}
