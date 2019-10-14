using System.IO;
using System.Threading.Tasks;

namespace Lifti.Serialization.Binary
{
    public interface IIndexSerializer<TKey>
    {
        Task SerializeAsync(IFullTextIndex<TKey> index, Stream stream, bool disposeStream = true);
    }
}