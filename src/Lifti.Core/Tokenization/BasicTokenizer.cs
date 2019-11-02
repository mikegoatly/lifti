using Lifti.Tokenization.Preprocessing;
using Lifti.Tokenization.Stemming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lifti.Tokenization
{
    public class BasicTokenizer : ConfiguredBy<TokenizationOptions>, ITokenizer
    {
        private readonly IInputPreprocessorPipeline inputPreprocessorPipeline = new InputPreprocessorPipeline();
        private TokenizationOptions tokenizationOptions = TokenizationOptions.Default;
        private HashSet<char> additionalSplitChars;
        private IWordStemmer stemmer;

        public IEnumerable<Token> Process(string input)
        {
            if (input == null)
            {
                return Enumerable.Empty<Token>();
            }

            return this.Process(input.AsSpan());
        }

        public IEnumerable<Token> Process(ReadOnlySpan<char> input)
        {
            var processedWords = new TokenStore();
            var wordIndex = 0;
            var start = 0;
            var wordBuilder = new StringBuilder();

            Process(processedWords, ref wordIndex, ref start, 0, wordBuilder, input);

            return processedWords.ToList();
        }

        public IEnumerable<Token> Process(IEnumerable<string> inputs)
        {
            if (inputs is null)
            {
                return Enumerable.Empty<Token>();
            }

            var processedWords = new TokenStore();
            var wordIndex = 0;
            var start = 0;
            var wordBuilder = new StringBuilder();
            var endOffset = 0;

            foreach (var input in inputs)
            {
                Process(processedWords, ref wordIndex, ref start, endOffset, wordBuilder, input.AsSpan());
                endOffset += input.Length;
            }

            return processedWords.ToList();
        }

        private void Process(
            TokenStore processedWords,
            ref int wordIndex,
            ref int start,
            int endOffset,
            StringBuilder wordBuilder,
            ReadOnlySpan<char> input)
        {
            for (var i = 0; i < input.Length; i++)
            {
                var current = input[i];
                if (this.IsWordSplitCharacter(current))
                {
                    if (wordBuilder.Length > 0)
                    {
                        this.CaptureWord(processedWords, wordIndex, start, i + endOffset, wordBuilder);
                        wordIndex++;
                        wordBuilder.Length = 0;
                    }

                    start = i + endOffset + 1;
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
                this.CaptureWord(processedWords, wordIndex, start, input.Length + endOffset, wordBuilder);
                wordIndex++;
                wordBuilder.Length = 0;
            }

            endOffset += input.Length;
            start = endOffset;
        }

        protected virtual bool IsWordSplitCharacter(char current)
        {
            return char.IsSeparator(current) ||
                char.IsControl(current) ||
                (this.tokenizationOptions.SplitOnPunctuation == true && char.IsPunctuation(current)) ||
                (this.additionalSplitChars?.Contains(current) == true);
        }

        
        private void CaptureWord(TokenStore processedWords, int wordIndex, int start, int end, StringBuilder wordBuilder)
        {
            var length = end - start;

            if (length > ushort.MaxValue)
            {
                throw new LiftiException($"Only words up to {ushort.MaxValue} characters long can be indexed");
            }

            if (this.stemmer != null)
            {
                this.stemmer.Stem(wordBuilder);
            }

            processedWords.MergeOrAdd(new TokenHash(wordBuilder), wordBuilder, new WordLocation(wordIndex, start, (ushort)length));
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
