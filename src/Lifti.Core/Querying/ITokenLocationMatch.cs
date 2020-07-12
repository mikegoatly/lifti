using System.Collections.Generic;

namespace Lifti.Querying
{
    public interface ITokenLocationMatch
    {
        int MaxTokenIndex { get; }
        int MinTokenIndex { get; }

        IEnumerable<TokenLocation> GetLocations();
    }
}
