using System;
using System.Collections.Generic;

namespace Lifti.Tokenization
{
    /// <summary>
    /// Provides equality comparison for <see cref="ReadOnlyMemory{T}"/> of <see cref="char"/> values.
    /// </summary>
    internal sealed class ReadOnlyMemoryCharComparer : IEqualityComparer<ReadOnlyMemory<char>>
    {
        public static readonly ReadOnlyMemoryCharComparer Instance = new();

        private ReadOnlyMemoryCharComparer()
        {
        }

        public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
        {
            return x.Span.SequenceEqual(y.Span);
        }

        public int GetHashCode(ReadOnlyMemory<char> obj)
        {
            var span = obj.Span;
            var hash = new HashCode();
            
            // Hash the characters in the span
            foreach (var ch in span)
            {
                hash.Add(ch);
            }
            
            return hash.ToHashCode();
        }
    }
}
