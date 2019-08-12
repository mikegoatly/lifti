using FluentAssertions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Lifti
{
    public class BasicSplitter : IWordSplitter
    {
        private readonly IInputPreprocessorPipeline inputPreprocessorPipeline;
        private WordSplitOptions wordSplitOptions = new WordSplitOptions();

        public BasicSplitter(IInputPreprocessorPipeline inputPreprocessorPipeline)
        {
            this.inputPreprocessorPipeline = inputPreprocessorPipeline;
        }

        public IEnumerable<SplitWord> Process(string input)
        {
            var processedWords = new SplitWordStore(); // TODO Pool?

            var inputData = input.AsSpan();
            var start = 0;
            var wordBuilder = new StringBuilder();
            for (var i = 0; i < inputData.Length; i++)
            {
                var current = input[i];
                if (this.IsWordSplitCharacter(current))
                {
                    if (wordBuilder.Length > 0)
                    {
                        CaptureWord(processedWords, inputData, start, i, wordBuilder);
                    }

                    start = i + 1;
                }
                else
                {
                    wordBuilder.Append(this.inputPreprocessorPipeline.Process(current));
                }
            }

            if (wordBuilder.Length > 0)
            {
                CaptureWord(processedWords, inputData, start, inputData.Length, wordBuilder);
            }

            return processedWords.ToList();
        }

        private bool IsWordSplitCharacter(char current)
        {
            return char.IsSeparator(current) ||
                (this.wordSplitOptions.SplitWordsOnPunctuation && char.IsPunctuation(current));
        }

        private static void CaptureWord(SplitWordStore processedWords, ReadOnlySpan<char> inputData, int start, int end, StringBuilder wordBuilder)
        {
            var length = end - start;
            var span = inputData.Slice(start, length);

            var hash = new SplitWordHash();
            for (var i = 0; i < wordBuilder.Length; i++)
            {
                hash.Combine(wordBuilder[i]);
            }

            processedWords.MergeOrAdd(hash, span, new Range(start, length));
        }

        public virtual void ConfigureWith(FullTextIndexOptions options)
        {
            this.wordSplitOptions = options.WordSplitOptions ?? throw new ArgumentNullException(nameof(options.WordSplitOptions));
        }
    }
}
