using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;

namespace Lifti.Tokenization
{
    /// <summary>
    /// Defines methods for processing inputs strings to a set of <see cref="Token"/> instances.
    /// </summary>
    public interface ITokenizer
    {
        /// <summary>
        /// Tokenizes the given <see cref="DocumentTextFragment"/>s relating a single document.
        /// </summary>
        IReadOnlyList<Token> Process(IEnumerable<DocumentTextFragment> input);

        /// <summary>
        /// Tokenizes a single string.
        /// </summary>
        IReadOnlyList<Token> Process(ReadOnlySpan<char> tokenText);

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