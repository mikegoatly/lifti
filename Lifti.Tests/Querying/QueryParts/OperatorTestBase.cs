using Lifti.Querying;
using System.Linq;

namespace Lifti.Tests.Querying.QueryParts
{
    public abstract class OperatorTestBase
    {
        protected static CompositeWordMatchLocation CompositeMatch(int leftWordIndex, int rightWordIndex)
        {
            return new CompositeWordMatchLocation(WordMatch(leftWordIndex), WordMatch(rightWordIndex));
        }

        protected static FieldMatch FieldMatch(byte fieldId, params int[] wordIndexes)
        {
            return new FieldMatch(
                    fieldId,
                    wordIndexes.Select(i => WordMatch(i)).ToList());
        }

        protected static IWordLocationMatch WordMatch(int index)
        {
            return new SingleWordLocationMatch(new WordLocation(index, index, index));
        }
    }
}
