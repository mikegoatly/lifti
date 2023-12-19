
using Lifti.Tokenization.Preprocessing;
using Lifti.Tokenization.Stemming;
using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lifti.Tokenization
{
    /// <summary>
    /// The default implementation of <see cref="IIndexTokenizer"/>.
    /// </summary>
    public class IndexTokenizer : IIndexTokenizer
    {
        private readonly InputPreprocessorPipeline inputPreprocessorPipeline;
        private readonly HashSet<char> additionalSplitChars;
        private readonly HashSet<char> ignoreChars;
        private readonly PorterStemmer? stemmer;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexTokenizer"/> class.
        /// </summary>
        /// <param name="tokenizationOptions">The tokenization options for this instance.</param>
        public IndexTokenizer(TokenizationOptions tokenizationOptions)
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
        /// Gets the default <see cref="IIndexTokenizer"/> implementation, configured with <see cref="TokenizationOptions.Default"/>.
        /// </summary>
        public static IndexTokenizer Default { get; } = new IndexTokenizer(TokenizationOptions.Default);

        /// <inheritdoc />
        public TokenizationOptions Options { get; }

        /// <inheritdoc />
        public IReadOnlyCollection<Token> Process(IEnumerable<DocumentTextFragment> input)
        {
            if (input is null)
            {
                return Array.Empty<Token>();
            }

            var processedTokens = new TokenStore();
            var tokenIndex = 0;
            var tokenBuilder = new StringBuilder();

            foreach (var documentFragment in input)
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
        public IReadOnlyCollection<Token> Process(ReadOnlySpan<char> input)
        {
            var processedTokens = new TokenStore();
            var tokenIndex = 0;
            var tokenBuilder = new StringBuilder();

            this.Process(input, ref tokenIndex, 0, processedTokens, tokenBuilder);

            return processedTokens.ToList();
        }

        /// <inheritdoc />
        public string Normalize(ReadOnlySpan<char> tokenText)
        {
            var tokenBuilder = new StringBuilder(tokenText.Length);

            foreach (var character in tokenText)
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
        public virtual bool IsSplitCharacter(char character)
        {
            return
                // Split when the character is well known as a Unicode separator or control character
                char.IsSeparator(character) || char.IsControl(character) || (
                    (
                        // Split if we are splitting on punctuation and the character is a punctuation character
                        (this.Options.SplitOnPunctuation == true && char.IsPunctuation(character)) ||
                        // Or the character is in the list of additional split characters
                        this.additionalSplitChars.Contains(character)
                    )
                    // Unless the character is an ignored characters
                    && this.ignoreChars.Contains(character) == false
               );
        }

        private void CaptureToken(TokenStore processedTokens, ref int tokenIndex, int start, int end, StringBuilder tokenBuilder)
        {
            var length = end - start;

            if (length > ushort.MaxValue)
            {
                throw new LiftiException(ExceptionMessages.MaxTokenLengthExceeded, ushort.MaxValue);
            }

            this.stemmer?.Stem(tokenBuilder);

            processedTokens.MergeOrAdd(tokenBuilder, new TokenLocation(tokenIndex, start, (ushort)length));

            tokenIndex++;
            tokenBuilder.Length = 0;
        }
    }
}
