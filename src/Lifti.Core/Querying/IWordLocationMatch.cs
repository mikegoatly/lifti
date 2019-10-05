using System.Collections.Generic;

namespace Lifti.Querying
{
    public interface IWordLocationMatch
    {
        int MaxWordIndex { get; }
        int MinWordIndex { get; }

        IEnumerable<WordLocation> GetLocations();
    }
}
