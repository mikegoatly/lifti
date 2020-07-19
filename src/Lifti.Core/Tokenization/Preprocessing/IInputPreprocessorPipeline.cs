using System.Collections.Generic;

namespace Lifti.Tokenization.Preprocessing
{
    /// <summary>
    /// Defines methods for pre-processing a <see cref="char"/> through a series
    /// of <see cref="IInputPreprocessor"/> instances.
    /// </summary>
    public interface IInputPreprocessorPipeline : IConfiguredBy<TokenizationOptions>
    {
        /// <summary>
        /// Processes the specified input character, passing it through a configured
        /// sequence of <see cref="IInputPreprocessor"/>s. One input character can 
        /// result in multiple characters being emitted, for example when normalizing
        /// the latin ǽ, two characters are returned, ae.
        /// </summary>
        IEnumerable<char> Process(char input);
    }
}
