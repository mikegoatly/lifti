using System;
using System.Collections.Generic;

namespace Lifti.Tokenization
{
    /// <summary>
    /// Defines methods for processing inputs strings to a set of <see cref="Token"/> instances
    /// that can be stored in an index.
    /// </summary>
    public interface ITokenizer : IConfiguredBy<TokenizationOptions>
    {
        /// <summary>
        /// Processes the given input to a set of <see cref="Token"/>. instances.
        /// </summary>
        IReadOnlyList<Token> Process(string input);

        /// <summary>
        /// Processes the given input to a set of <see cref="Token"/>. instances.
        /// </summary>
        IReadOnlyList<Token> Process(ReadOnlySpan<char> input);

        /// <summary>
        /// Processes the given input to a set of <see cref="Token"/>. instances.
        /// </summary>
        IReadOnlyList<Token> Process(IEnumerable<string> input);
    }
}