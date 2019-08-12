using FluentAssertions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Lifti
{
    public class BasicTokenizer : ITokenizer
    {
        private readonly IInputPreprocessorPipeline inputPreprocessorPipeline;
        private TokenizationOptions tokenizationOptions = new TokenizationOptions();

        public BasicTokenizer(IInputPreprocessorPipeline inputPreprocessorPipeline)
        {
            this.inputPreprocessorPipeline = inputPreprocessorPipeline;
        }

        public IEnumerable<Token> Process(string input)
        {
            var processedWords = new TokenStore(); // TODO Pool?

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
                (this.tokenizationOptions.SplitOnPunctuation && char.IsPunctuation(current));
        }

        private static void CaptureWord(TokenStore processedWords, ReadOnlySpan<char> inputData, int start, int end, StringBuilder wordBuilder)
        {
            var length = end - start;
            var span = inputData.Slice(start, length);

            var hash = new TokenHash();
            for (var i = 0; i < wordBuilder.Length; i++)
            {
                hash.Combine(wordBuilder[i]);
            }

            processedWords.MergeOrAdd(hash, span, new Range(start, length));
        }

        public virtual void ConfigureWith(FullTextIndexOptions options)
        {
            this.tokenizationOptions = options.TokenizationOptions ?? throw new ArgumentNullException(nameof(options.TokenizationOptions));
        }
    }
}
