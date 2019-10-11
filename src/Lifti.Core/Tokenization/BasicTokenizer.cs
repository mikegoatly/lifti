using Lifti.Tokenization.Preprocessing;
using Lifti.Tokenization.Stemming;
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

        public IEnumerable<Token> Process(string input)
        {
            return this.Process(input.AsSpan());
        }

        public IEnumerable<Token> Process(ReadOnlySpan<char> input)
        {
            var processedWords = new TokenStore(); // TODO Pool?

            var wordIndex = 0;
            var start = 0;
            var wordBuilder = new StringBuilder();
            for (var i = 0; i < input.Length; i++)
            {
                var current = input[i];
                if (this.IsWordSplitCharacter(current))
                {
                    if (wordBuilder.Length > 0)
                    {
                        this.CaptureWord(processedWords, wordIndex, start, i, wordBuilder);
                        wordIndex++;
                        wordBuilder.Length = 0;
                    }

                    start = i + 1;
                }
                else
                {
                    foreach (var processed in this.inputPreprocessorPipeline.Process(current))
                    {
                        wordBuilder.Append(processed);
                    }
                }
            }

            if (wordBuilder.Length > 0)
            {
                this.CaptureWord(processedWords, wordIndex, start, input.Length, wordBuilder);
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

        private readonly PorterStemmer stemmer = new PorterStemmer();
        private void CaptureWord(TokenStore processedWords, int wordIndex, int start, int end, StringBuilder wordBuilder)
        {
            var length = end - start;
            this.stemmer.Stem(wordBuilder);
            processedWords.MergeOrAdd(new TokenHash(wordBuilder), wordBuilder, new WordLocation(wordIndex, start, length));
        }

        protected override void OnConfiguring(TokenizationOptions options)
        {
            this.tokenizationOptions = options;

            this.additionalSplitChars = this.tokenizationOptions.AdditionalSplitCharacters?.Count > 0
                ? new HashSet<char>(this.tokenizationOptions.AdditionalSplitCharacters)
                : null;

            this.inputPreprocessorPipeline.Configure(options);
        }
    }
}
