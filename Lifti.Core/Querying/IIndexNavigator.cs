using System;

namespace Lifti.Querying
{
    public interface IIndexNavigator
    {
        IntermediateQueryResult GetExactMatches();
        bool Process(char next);
        bool Process(ReadOnlySpan<char> text);
    }
}