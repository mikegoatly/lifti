using Lifti.Tokenization;

namespace Lifti.Querying
{
    public interface IQueryParser
    {
        IQuery Parse(string queryText, ITokenizer wordTokenizer);
    }
}
