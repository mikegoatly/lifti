using System.Collections.Generic;

namespace Lifti.Tokenization
{
    /// <summary>
    /// Provides words that are logically equivalent to others.
    /// </summary>
    public interface IThesaurus
    {
        /// <summary>
        /// Processes the given <see cref="Token"/>, returning a set of tokens that
        /// are synonymns. The list will always include the original token.
        /// </summary>
        IEnumerable<Token> Process(Token token);
    }
}