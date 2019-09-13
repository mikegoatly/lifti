using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lifti
{
    public class BasicTokenizer : ITokenizer
    {
        private readonly IInputPreprocessorPipeline inputPreprocessorPipeline;
        private TokenizationOptions tokenizationOptions = new TokenizationOptions();
        private HashSet<char> additionalSplitChars;

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
            var hash = new TokenHash();
            for (var i = 0; i < inputData.Length; i++)
            {
                var current = input[i];
                if (this.IsWordSplitCharacter(current))
                {
                    if (wordBuilder.Length > 0)
                    {
                        CaptureWord(processedWords, hash, start, i, wordBuilder);
                        wordBuilder.Length = 0;
                        hash = new TokenHash();
                    }

                    start = i + 1;
                }
                else
                {
                    foreach (var processed in this.inputPreprocessorPipeline.Process(current))
                    {
                        wordBuilder.Append(processed);
                        hash = hash.Combine(processed);
                    }
                }
            }

            if (wordBuilder.Length > 0)
            {
                CaptureWord(processedWords, hash, start, inputData.Length, wordBuilder);
            }

            return processedWords.ToList();
        }

        protected virtual bool IsWordSplitCharacter(char current)
        {
            return char.IsSeparator(current) ||
                char.IsControl(current) ||
                (this.tokenizationOptions.SplitOnPunctuation && char.IsPunctuation(current)) ||
                (this.additionalSplitChars?.Contains(current) == true);
        }

        private static void CaptureWord(TokenStore processedWords, TokenHash hash, int start, int end, StringBuilder wordBuilder)
        {
            var length = end - start;
            processedWords.MergeOrAdd(hash, wordBuilder, new Range(start, length));
        }

        public virtual void ConfigureWith(FullTextIndexOptions options)
        {
            this.tokenizationOptions = options.TokenizationOptions ?? throw new ArgumentNullException(nameof(options.TokenizationOptions));
            this.additionalSplitChars = this.tokenizationOptions.AdditionalSplitCharacters?.Length > 0
                ? new HashSet<char>(this.tokenizationOptions.AdditionalSplitCharacters)
                : null;
        }
    }
}
