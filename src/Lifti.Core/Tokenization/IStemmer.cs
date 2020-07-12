using System.Text;

namespace Lifti.Tokenization
{
    internal interface IStemmer
    {
        void Stem(StringBuilder builder);
    }
}