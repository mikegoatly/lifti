using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    public struct CompositeWordMatchLocation : IWordLocationMatch, IEquatable<CompositeWordMatchLocation>
    {
        private readonly IWordLocationMatch leftWord;
        private readonly IWordLocationMatch rightWord;
        private readonly Lazy<int> minWordIndex;
        private readonly Lazy<int> maxWordIndex;

        public CompositeWordMatchLocation(IWordLocationMatch leftWord, IWordLocationMatch rightWord)
        {
            this.leftWord = leftWord;
            this.rightWord = rightWord;
            this.minWordIndex = new Lazy<int>(() => Math.Max(leftWord.MinWordIndex, rightWord.MinWordIndex));
            this.maxWordIndex = new Lazy<int>(() => Math.Max(leftWord.MaxWordIndex, rightWord.MaxWordIndex));
        }

        public int MaxWordIndex => this.maxWordIndex.Value;

        public int MinWordIndex => this.minWordIndex.Value;

        public override bool Equals(object obj)
        {
            return obj is CompositeWordMatchLocation location &&
                   this.Equals(location);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.leftWord, this.rightWord);
        }

        public IEnumerable<WordLocation> GetLocations()
        {
            return this.leftWord.GetLocations().Concat(this.rightWord.GetLocations());
        }

        public bool Equals(CompositeWordMatchLocation other)
        {
            return this.leftWord.Equals(other.leftWord) &&
                   this.rightWord.Equals(other.rightWord);
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