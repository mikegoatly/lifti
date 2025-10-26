using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tokenization
{
    internal static class TokenExtensions
    {
        /// <summary>
        /// Calculates field statistics (token count and last token index) in a single pass.
        /// </summary>
        /// <param name="tokens">The tokens to process.</param>
        /// <returns>
        /// A <see cref="FieldStatistics"/> containing the total token count and the maximum token index,
        /// or -1 for last token index if there are no tokens.
        /// </returns>
        internal static FieldStatistics CalculateFieldStatistics(this IList<Token> tokens)
        {
            if (tokens.Count == 0)
            {
                return new FieldStatistics(0, -1);
            }

            var totalCount = 0;
            var maxTokenIndex = -1;

            for (var i = 0; i < tokens.Count; i++)
            {
                var locations = tokens[i].Locations;
                totalCount += locations.Count;

                if (locations.Count > 0)
                {
                    maxTokenIndex = Math.Max(
                        maxTokenIndex,
                        locations.Max(loc => loc.TokenIndex));
                }
            }

            return new FieldStatistics(totalCount, maxTokenIndex);
        }
    }
}
