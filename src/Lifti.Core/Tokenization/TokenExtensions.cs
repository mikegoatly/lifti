using System.Collections.Generic;

namespace Lifti.Tokenization
{
    internal static class TokenExtensions
    {
        internal static int CalculateTotalTokenCount(this IList<Token> tokens)
        {
            var totalCount = 0;
            for (var i = 0; i < tokens.Count; i++)
            {
                totalCount += tokens[i].Locations.Count;
            }

            return totalCount;
        }
    }
}
