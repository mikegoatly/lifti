using System;

namespace Lifti.Querying
{
    public interface IIndexNavigator
    {
        IntermediateQueryResult GetExactMatches();
        bool Process(char value);
        bool Process(ReadOnlySpan<char> text);
    }
}