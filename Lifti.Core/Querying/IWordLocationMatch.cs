using System.Collections.Generic;

namespace Lifti.Querying
{
    public interface IWordLocationMatch
    {
        IEnumerable<WordLocation> GetLocations();
    }
}
