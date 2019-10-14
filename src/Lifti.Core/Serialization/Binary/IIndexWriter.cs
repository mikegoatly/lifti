using System;
using System.IO;
using System.Threading.Tasks;

namespace Lifti.Serialization.Binary
{
    internal interface IIndexWriter<TKey> : IDisposable
    {
        Task WriteAsync(IFullTextIndex<TKey> index);
    }
}