using Lifti.Tokenization.Preprocessing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lifti.Tokenization
{
    public class BasicTokenizer : ConfiguredBy<TokenizationOptions>, ITokenizer
    {
        private readonly IInputPreprocessorPipeline inputPreprocessorPipeline = new InputPreprocessorPipeline();
        private TokenizationOptions tokenizationOptions;
        private HashSet<char> additionalSplitChars;

        public IEnumerable<Token> Process(ReadOnlySpan<char> input)
        {
            var processedWords = new TokenStore(); // TODO Pool?

            var start = 0;
            var wordBuilder = new StringBuilder();
            var hash = new TokenHash();
            for (var i = 0; i < input.Length; i++)
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
                CaptureWord(processedWords, hash, start, input.Length, wordBuilder);
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

        protected override void OnConfiguring(TokenizationOptions options)
        {
            this.tokenizationOptions = options;

            this.additionalSplitChars = this.tokenizationOptions.AdditionalSplitCharacters?.Length > 0
                ? new HashSet<char>(this.tokenizationOptions.AdditionalSplitCharacters)
                : null;

            this.inputPreprocessorPipeline.Configure(options);
        }
    }
}
