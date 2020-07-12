using System;

namespace Lifti.Tokenization
{
    internal static class SpanExtensions
    {
        public static bool SequenceEqual(this ReadOnlySpan<char> text, string chars)
        {
            if (chars.Length != text.Length)
            {
                return false;
            }

            for (var i = 0; i < chars.Length; i++)
            {
                if (chars[i] != text[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
