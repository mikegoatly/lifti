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
        /// Gets the options applied to this instance.
        /// </summary>
        TokenizationOptions Options { get; }

        /// <summary>
        /// Tokenizes the given <see cref="DocumentTextFragment"/>s relating a single document.
        /// </summary>
        IReadOnlyList<Token> Process(IEnumerable<DocumentTextFragment> input);

        /// <summary>
        /// Tokenizes a single string.
        /// </summary>
        IReadOnlyList<Token> Process(ReadOnlySpan<char> tokenText);
    }
}