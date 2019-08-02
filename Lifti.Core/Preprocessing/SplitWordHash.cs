using System;

namespace Lifti
{
    public struct SplitWordHash
    {
        public SplitWordHash(ReadOnlySpan<char> word)
        {
            var hash = 0;
            for (var i = 0; i < word.Length; i++)
            {
                hash = CalculateNext(hash, word[i]);
            }

            this.HashValue = hash;
        }

        public SplitWordHash(int hashValue)
        {
            this.HashValue = hashValue;
        }

        public int HashValue { get; }

        public SplitWordHash Combine(char next)
        {
            return new SplitWordHash(CalculateNext(this.HashValue, next));
        }

        private static int CalculateNext(int current, char next)
        {
            return HashCode.Combine(current, next);
        }

        public override bool Equals(object obj)
        {
            return obj is SplitWordHash hash &&
                   this.HashValue == hash.HashValue;
        }

        public override int GetHashCode()
        {
            return this.HashValue;
        }
    }
}
