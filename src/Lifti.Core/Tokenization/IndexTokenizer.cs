
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
        private readonly IStemmer? stemmer;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexTokenizer"/> class.
        /// </summary>
        /// <param name="tokenizationOptions">The tokenization options for this instance.</param>
        public IndexTokenizer(TokenizationOptions tokenizationOptions)
        {
            this.Options = tokenizationOptions ?? throw new ArgumentNullException(nameof(tokenizationOptions));
            this.stemmer = tokenizationOptions.Stemmer;

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
            var tokenBuffer = new CharacterBuffer(256);

            try
            {
                foreach (var documentFragment in input)
                {
                    this.Process(
                        documentFragment.Text.Span,
                        ref tokenIndex,
                        documentFragment.Offset,
                        processedTokens,
                        ref tokenBuffer);
                }

                return processedTokens.ToList();
            }
            finally
            {
                tokenBuffer.Dispose();
            }
        }

        /// <inheritdoc />
        public IReadOnlyCollection<Token> Process(ReadOnlySpan<char> input)
        {
            var processedTokens = new TokenStore();
            var tokenIndex = 0;
            var tokenBuffer = new CharacterBuffer(256);

            try
            {
                this.Process(input, ref tokenIndex, 0, processedTokens, ref tokenBuffer);

                return processedTokens.ToList();
            }
            finally
            {
                tokenBuffer.Dispose();
            }
        }

        /// <inheritdoc />
        public string Normalize(ReadOnlySpan<char> tokenText)
        {
            var tokenBuffer = new CharacterBuffer(tokenText.Length);

            try
            {
                foreach (var character in tokenText)
                {
                    foreach (var processed in this.inputPreprocessorPipeline.Process(character))
                    {
                        tokenBuffer.Append(processed);
                    }
                }

                return tokenBuffer.ToString();
            }
            finally
            {
                tokenBuffer.Dispose();
            }
        }

        private void Process(
            ReadOnlySpan<char> input,
            ref int tokenIndex,
            int startOffset,
            TokenStore processedTokens,
            ref CharacterBuffer tokenBuffer)
        {
            var start = startOffset;
            for (var i = 0; i < input.Length; i++)
            {
                var current = input[i];
                if (this.IsSplitCharacter(current))
                {
                    if (tokenBuffer.Length > 0)
                    {
                        this.CaptureToken(processedTokens, ref tokenIndex, start, i + startOffset, ref tokenBuffer);
                    }

                    start = i + startOffset + 1;
                }
                else
                {
                    foreach (var processed in this.inputPreprocessorPipeline.Process(current))
                    {
                        tokenBuffer.Append(processed);
                    }
                }
            }

            if (tokenBuffer.Length > 0)
            {
                this.CaptureToken(processedTokens, ref tokenIndex, start, input.Length + startOffset, ref tokenBuffer);
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

        private void CaptureToken(TokenStore processedTokens, ref int tokenIndex, int start, int end, ref CharacterBuffer tokenBuffer)
        {
            var length = end - start;

            if (length > ushort.MaxValue)
            {
                throw new LiftiException(ExceptionMessages.MaxTokenLengthExceeded, ushort.MaxValue);
            }

            this.stemmer?.Stem(ref tokenBuffer);

            processedTokens.MergeOrAdd(tokenBuffer.AsMemory(), new TokenLocation(tokenIndex, start, (ushort)length));

            tokenIndex++;
            tokenBuffer.Clear();
        }
    }
}
