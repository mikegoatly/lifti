using System;
using System.Threading.Tasks;

namespace Lifti.Serialization.Binary
{
    public interface IIndexReader<TKey> : IDisposable
    {
        Task ReadIntoAsync(IFullTextIndex<TKey> index);
    }
}