using Lifti.Tokenization;

namespace Lifti.Querying
{
    public interface IQueryParser
    {
        IQuery Parse(IIndexedFieldLookup fieldLookup, string queryText, ITokenizer wordTokenizer);
    }
}
