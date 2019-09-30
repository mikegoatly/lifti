using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Lifti.Querying
{
    public struct CompositeWordMatchLocation : IWordLocationMatch, IEquatable<CompositeWordMatchLocation>
    {
        private readonly IWordLocationMatch currentWord;
        private readonly IWordLocationMatch nextWord;

        public CompositeWordMatchLocation(IWordLocationMatch currentWord, IWordLocationMatch nextWord)
        {
            this.currentWord = currentWord;
            this.nextWord = nextWord;
        }

        public int MaxWordIndex => Math.Max(this.currentWord.MaxWordIndex, this.nextWord.MaxWordIndex);

        public int MinWordIndex => Math.Min(this.currentWord.MaxWordIndex, this.nextWord.MaxWordIndex);

        public override bool Equals(object obj)
        {
            return obj is CompositeWordMatchLocation location &&
                   this.Equals(location);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.currentWord, this.nextWord);
        }

        public IEnumerable<WordLocation> GetLocations()
        {
            return this.currentWord.GetLocations().Concat(this.nextWord.GetLocations());
        }

        public bool Equals(CompositeWordMatchLocation other)
        {
            return this.currentWord.Equals(other.currentWord) &&
                   this.nextWord.Equals(other.nextWord);
        }

        public static bool operator ==(CompositeWordMatchLocation left, CompositeWordMatchLocation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CompositeWordMatchLocation left, CompositeWordMatchLocation right)
        {
            return !(left == right);
        }
    }
}