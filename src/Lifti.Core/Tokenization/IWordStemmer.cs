using System.Text;

namespace Lifti.Tokenization
{
    internal interface IWordStemmer
    {
        void Stem(StringBuilder builder);
    }
}