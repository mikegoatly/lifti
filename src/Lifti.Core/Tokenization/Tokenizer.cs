
using Lifti.Tokenization.Preprocessing;
using Lifti.Tokenization.Stemming;
using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Lifti.Tokenization
{
    /// <summary>
    /// The default implementation of <see cref="ITokenizer"/> that just extracts tokens from plain text.
    /// </summary>
    public class Tokenizer : ITokenizer
    {
        private readonly IInputPreprocessorPipeline inputPreprocessorPipeline;
        private readonly HashSet<char>? additionalSplitChars;
        private readonly IStemmer? stemmer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tokenizer"/> class.
        /// </summary>
        /// <param name="tokenizationOptions">The tokenization options for this instance.</param>
        public Tokenizer(TokenizationOptions tokenizationOptions)
        {
            this.Options = tokenizationOptions ?? throw new ArgumentNullException(nameof(tokenizationOptions));

            if (tokenizationOptions.Stemming)
            {
                this.stemmer = new PorterStemmer();
            }

            this.additionalSplitChars = tokenizationOptions.AdditionalSplitCharacters.Count > 0
                ? new HashSet<char>(tokenizationOptions.AdditionalSplitCharacters)
                : null;

            this.inputPreprocessorPipeline = new InputPreprocessorPipeline(tokenizationOptions);
        }

        /// <summary>
        /// Gets the default <see cref="ITokenizer"/> implementation, configured with <see cref="TokenizationOptions.Default"/>.
        /// </summary>
        public static Tokenizer Default { get; } = new Tokenizer(TokenizationOptions.Default);

        /// <inheritdoc />
        public TokenizationOptions Options { get; }

        /// <inheritdoc />
        public IReadOnlyList<Token> Process(IEnumerable<DocumentTextFragment> document)
        {
            if (document is null)
            {
                return Array.Empty<Token>();
            }

            var processedTokens = new TokenStore();
            var tokenIndex = 0;
            var tokenBuilder = new StringBuilder();

            foreach (var documentFragment in document)
            {
                this.Process(
                    processedTokens,
                    ref tokenIndex,
                    documentFragment.Offset,
                    tokenBuilder,
                    documentFragment.Text.Span);
            }

            return processedTokens.ToList();
        }

        /// <inheritdoc />
        public IReadOnlyList<Token> Process(ReadOnlySpan<char> text)
        {
            var processedTokens = new TokenStore();
            var tokenIndex = 0;
            var tokenBuilder = new StringBuilder();

            this.Process(processedTokens, ref tokenIndex, 0, tokenBuilder, text);

            return processedTokens.ToList();
        }

        private void Process(
            TokenStore processedTokens,
            ref int tokenIndex,
            int startOffset,
            StringBuilder tokenBuilder,
            ReadOnlySpan<char> input)
        {
            var start = startOffset;
            for (var i = 0; i < input.Length; i++)
            {
                var current = input[i];
                if (this.IsSplitCharacter(current))
                {
                    if (tokenBuilder.Length > 0)
                    {
                        this.CaptureToken(processedTokens, tokenIndex, start, i + startOffset, tokenBuilder);
                        tokenIndex++;
                        tokenBuilder.Length = 0;
                    }

                    start = i + startOffset + 1;
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
                this.CaptureToken(processedTokens, tokenIndex, start, input.Length + startOffset, tokenBuilder);
                tokenIndex++;
                tokenBuilder.Length = 0;
            }
        }

        /// <summary>
        /// Determines whether the given character is considered to be a word splitting character.
        /// </summary>
        protected virtual bool IsSplitCharacter(char current)
        {
            return char.IsSeparator(current) ||
                char.IsControl(current) ||
                (this.Options.SplitOnPunctuation == true && char.IsPunctuation(current)) ||
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
    }
}
