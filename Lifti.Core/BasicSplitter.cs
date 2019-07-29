using System;
using System.Collections.Generic;

namespace Lifti
{
    public class BasicSplitter
    {
        public IEnumerable<SplitWord> Process(string input)
        {
            input = input.ToLowerInvariant();

            var processedWords = new SplitWordStore(); // TODO Pool?

            var inputData = input.AsSpan();
            var start = 0;
            var foundCharacter = false;
            var hash = new SplitWordHash();
            for (var i = 0; i < inputData.Length; i++)
            {
                var current = input[i];
                if (current == ' ')
                {
                    if (foundCharacter)
                    {
                        CaptureWord(processedWords, inputData, start, i, hash);

                        foundCharacter = false;
                        hash = new SplitWordHash();
                    }

                    start = i + 1;
                }
                else
                {
                    foundCharacter = true;
                    unchecked
                    {
                        hash = hash.Combine(current);
                    }
                }
            }

            if (foundCharacter)
            {
                CaptureWord(processedWords, inputData, start, inputData.Length, hash);
            }

            return processedWords.ToList();
        }

        private static void CaptureWord(SplitWordStore processedWords, ReadOnlySpan<char> inputData, int start, int end, SplitWordHash hash)
        {
            var length = end - start;
            var span = inputData.Slice(start, length);

            processedWords.MergeOrAdd(hash, span, new Range(start, length));
        }
    }
}
