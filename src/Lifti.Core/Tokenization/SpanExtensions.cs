using System;

namespace Lifti.Tokenization
{
    public static class SpanExtensions
    {
        public static bool SequenceEqual(this ReadOnlySpan<char> word, string chars)
        {
            if (chars is null)
            {
                throw new ArgumentNullException(nameof(chars));
            }

            if (chars.Length != word.Length)
            {
                return false;
            }

            for (var i = 0; i < chars.Length; i++)
            {
                if (chars[i] != word[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
