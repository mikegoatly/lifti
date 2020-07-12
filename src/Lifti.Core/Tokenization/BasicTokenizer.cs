using Lifti.Tokenization.Preprocessing;
using Lifti.Tokenization.Stemming;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace Lifti.Tokenization
{
    public class BasicTokenizer : ConfiguredBy<TokenizationOptions>, ITokenizer
    {
        private readonly IInputPreprocessorPipeline inputPreprocessorPipeline = new InputPreprocessorPipeline();
        private TokenizationOptions tokenizationOptions = TokenizationOptions.Default;
        private HashSet<char>? additionalSplitChars;
        private IStemmer? stemmer;

        public IReadOnlyList<Token> Process(string input)
        {
            if (input == null)
            {
                return ImmutableList<Token>.Empty;
            }

            return this.Process(input.AsSpan());
        }

        public IReadOnlyList<Token> Process(ReadOnlySpan<char> input)
        {
            var processedTokens = new TokenStore();
            var tokenIndex = 0;
            var start = 0;
            var tokenBuilder = new StringBuilder();

            this.Process(processedTokens, ref tokenIndex, ref start, 0, tokenBuilder, input);

            return processedTokens.ToList();
        }

        public IReadOnlyList<Token> Process(IEnumerable<string> inputs)
        {
            if (inputs is null)
            {
                return ImmutableList<Token>.Empty;
            }

            var processedTokens = new TokenStore();
            var tokenIndex = 0;
            var start = 0;
            var tokenBuilder = new StringBuilder();
            var endOffset = 0;

            foreach (var input in inputs)
            {
                this.Process(processedTokens, ref tokenIndex, ref start, endOffset, tokenBuilder, input.AsSpan());
                endOffset += input.Length;
            }

            return processedTokens.ToList();
        }

        private void Process(
            TokenStore processedTokens,
            ref int tokenIndex,
            ref int start,
            int endOffset,
            StringBuilder tokenBuilder,
            ReadOnlySpan<char> input)
        {
            for (var i = 0; i < input.Length; i++)
            {
                var current = input[i];
                if (this.IsSplitCharacter(current))
                {
                    if (tokenBuilder.Length > 0)
                    {
                        this.CaptureToken(processedTokens, tokenIndex, start, i + endOffset, tokenBuilder);
                        tokenIndex++;
                        tokenBuilder.Length = 0;
                    }

                    start = i + endOffset + 1;
                }
                else
                {
                    foreach (var processed in this.inputPreprocessorPipeline.Process(current))
                    {
                        tokenBuilder.Append(processed);
                    }
                }
            }

            if (tokenBuilder.Length > 0)
            {
                this.CaptureToken(processedTokens, tokenIndex, start, input.Length + endOffset, tokenBuilder);
                tokenIndex++;
                tokenBuilder.Length = 0;
            }

            endOffset += input.Length;
            start = endOffset;
        }

        protected virtual bool IsSplitCharacter(char current)
        {
            return char.IsSeparator(current) ||
                char.IsControl(current) ||
                (this.tokenizationOptions.SplitOnPunctuation == true && char.IsPunctuation(current)) ||
                (this.additionalSplitChars?.Contains(current) == true);
        }


        private void CaptureToken(TokenStore processedTokens, int tokenIndex, int start, int end, StringBuilder tokenBuilder)
        {
            var length = end - start;

            if (length > ushort.MaxValue)
            {
                throw new LiftiException(string.Format(CultureInfo.InvariantCulture, ExceptionMessages.MaxTokenLengthExceeded, ushort.MaxValue));
            }

            if (this.stemmer != null)
            {
                this.stemmer.Stem(tokenBuilder);
            }

            processedTokens.MergeOrAdd(new TokenHash(tokenBuilder), tokenBuilder, new TokenLocation(tokenIndex, start, (ushort)length));
        }

        protected override void OnConfiguring(TokenizationOptions options)
        {
            this.tokenizationOptions = options ?? throw new ArgumentNullException(nameof(options));

            if (this.tokenizationOptions.Stemming)
            {
                this.stemmer = new PorterStemmer();
            }

            this.additionalSplitChars = this.tokenizationOptions.AdditionalSplitCharacters.Count > 0
                ? new HashSet<char>(this.tokenizationOptions.AdditionalSplitCharacters)
                : null;

            this.inputPreprocessorPipeline.Configure(options);
        }
    }
}
