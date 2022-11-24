
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
        private readonly HashSet<char> additionalSplitChars;
        private readonly HashSet<char> ignoreChars;
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

            this.additionalSplitChars = new HashSet<char>(tokenizationOptions.AdditionalSplitCharacters);
            this.ignoreChars = new HashSet<char>(tokenizationOptions.IgnoreCharacters);

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
                    documentFragment.Text.Span,
                    ref tokenIndex,
                    documentFragment.Offset,
                    processedTokens,
                    tokenBuilder);
            }

            return processedTokens.ToList();
        }

        /// <inheritdoc />
        public IReadOnlyList<Token> Process(ReadOnlySpan<char> text)
        {
            var processedTokens = new TokenStore();
            var tokenIndex = 0;
            var tokenBuilder = new StringBuilder();

            this.Process(text, ref tokenIndex, 0, processedTokens, tokenBuilder);

            return processedTokens.ToList();
        }

        /// <inheritdoc />
        public string Normalize(ReadOnlySpan<char> text)
        {
            var tokenBuilder = new StringBuilder(text.Length);

            foreach (var character in text)
            {
                foreach (var processed in this.inputPreprocessorPipeline.Process(character))
                {
                    tokenBuilder.Append(processed);
                }
            }

            return tokenBuilder.ToString();
        }

        private void Process(
            ReadOnlySpan<char> input,
            ref int tokenIndex,
            int startOffset,
            TokenStore processedTokens,
            StringBuilder tokenBuilder)
        {
            var start = startOffset;
            for (var i = 0; i < input.Length; i++)
            {
                var current = input[i];
                if (this.IsSplitCharacter(current))
                {
                    if (tokenBuilder.Length > 0)
                    {
                        this.CaptureToken(processedTokens, ref tokenIndex, start, i + startOffset, tokenBuilder);
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
                this.CaptureToken(processedTokens, ref tokenIndex, start, input.Length + startOffset, tokenBuilder);
            }
        }

        /// <summary>
        /// Determines whether the given character is considered to be a word splitting character.
        /// </summary>
        public virtual bool IsSplitCharacter(char current)
        {
            return
                // Split when the character is well known as a Unicode separator or control character
                char.IsSeparator(current) || char.IsControl(current) || (
                    (
                        // Split if we are splitting on punctuation and the character is a punctuation character
                        (this.Options.SplitOnPunctuation == true && char.IsPunctuation(current)) ||
                        // Or the character is in the list of additional split characters
                        this.additionalSplitChars.Contains(current)
                    )
                    // Unless the character is an ignored characters
                    && this.ignoreChars.Contains(current) == false
               );
        }

        private void CaptureToken(TokenStore processedTokens, ref int tokenIndex, int start, int end, StringBuilder tokenBuilder)
        {
            var length = end - start;

            if (length > ushort.MaxValue)
            {
                throw new LiftiException(string.Format(CultureInfo.InvariantCulture, ExceptionMessages.MaxTokenLengthExceeded, ushort.MaxValue));
            }

            this.stemmer?.Stem(tokenBuilder);

            processedTokens.MergeOrAdd(tokenBuilder, new TokenLocation(tokenIndex, start, (ushort)length));

            tokenIndex++;
            tokenBuilder.Length = 0;
        }
    }
}
