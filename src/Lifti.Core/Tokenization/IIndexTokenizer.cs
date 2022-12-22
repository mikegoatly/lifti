using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;

namespace Lifti.Tokenization
{
    /// <summary>
    /// Defines methods for processing text for use in an index.
    /// </summary>
    public interface IIndexTokenizer
    {
        /// <summary>
        /// Tokenizes the given <see cref="DocumentTextFragment"/>s relating a single document.
        /// </summary>
        IReadOnlyCollection<Token> Process(IEnumerable<DocumentTextFragment> input);

        /// <summary>
        /// Tokenizes a single string.
        /// </summary>
        IReadOnlyCollection<Token> Process(ReadOnlySpan<char> tokenText);

        /// <summary>
        /// Normalizes a fragment of text according to the rules in the tokenizer.
        /// </summary>
        string Normalize(ReadOnlySpan<char> tokenText);

        /// <summary>
        /// Returns whether this tokenizer considers the given character to be a split character.
        /// </summary>
        bool IsSplitCharacter(char character);
    }
}