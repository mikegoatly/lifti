using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lifti
{
    /// <summary>
    /// This is not thread safe!
    /// </summary>
    internal class VirtualString
    {
        private readonly IList<ReadOnlyMemory<char>> strings;
        private readonly int length;
        private readonly StringBuilder stringBuilder;

        public VirtualString(IEnumerable<ReadOnlyMemory<char>> strings)
        {
            this.strings = strings as IList<ReadOnlyMemory<char>> ?? [.. strings];
            this.length = this.strings.Sum(s => s.Length);
            this.stringBuilder = new StringBuilder();
        }

        public string Substring(int start, int length)
        {
            if (start > this.length)
            {
                return string.Empty;
            }

#if NET8_0_OR_GREATER
            ArgumentOutOfRangeException.ThrowIfNegative(start);
#else 
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }
#endif
            if (start + length > this.length)
            {
                // Pin the requested length to the maximum length it can be
                length = this.length - start;
            }

            foreach (var currentString in this.strings)
            {
                var currentLength = currentString.Length;

                // Is the start index is within the current string
                if (start < currentLength)
                {
                    // Calculate the substring length for the current string
                    var substringLength = Math.Min(currentLength - start, length);
                    this.stringBuilder.Append(currentString.Span.Slice(start, substringLength));

                    // Update the start index and length for the next string
                    start = 0;
                    length -= substringLength;
                }
                else
                {
                    // update the start index for the next string
                    start -= currentLength;
                }

                // Have we finished reading the required length?
                if (length == 0)
                {
                    break;
                }
            }

            var result = this.stringBuilder.ToString();

            // Reset the string builder state for the next use
            this.stringBuilder.Length = 0;

            return result;
        }
    }

}
