using System;

namespace Lifti.Preprocessing
{
    public struct TokenHash
    {
        public TokenHash(ReadOnlySpan<char> word)
        {
            var hash = 0;
            for (var i = 0; i < word.Length; i++)
            {
                hash = CalculateNext(hash, word[i]);
            }

            this.HashValue = hash;
        }

        public TokenHash(int hashValue)
        {
            this.HashValue = hashValue;
        }

        public int HashValue { get; }

        public TokenHash Combine(char next)
        {
            return new TokenHash(CalculateNext(this.HashValue, next));
        }

        private static int CalculateNext(int current, char next)
        {
            return HashCode.Combine(current, next);
        }

        public override bool Equals(object obj)
        {
            return obj is TokenHash hash &&
                   this.HashValue == hash.HashValue;
        }

        public override int GetHashCode()
        {
            return this.HashValue;
        }
    }
}
