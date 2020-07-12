using System;
using System.Text;

namespace Lifti.Tokenization
{
    internal struct TokenHash : IEquatable<TokenHash>
    {
        public TokenHash(ReadOnlySpan<char> token)
        {
            var hash = 0;
            for (var i = 0; i < token.Length; i++)
            {
                hash = CalculateNext(hash, token[i]);
            }

            this.HashValue = hash;
        }

        public TokenHash(StringBuilder token)
        {
            var hash = 0;
            for (var i = 0; i < token.Length; i++)
            {
                hash = CalculateNext(hash, token[i]);
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
                   this.Equals(hash);
        }

        public override int GetHashCode()
        {
            return this.HashValue;
        }

        public bool Equals(TokenHash other)
        {
            return this.HashValue == other.HashValue;
        }
    }
}
