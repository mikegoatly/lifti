using System;
using System.Threading.Tasks;

namespace Lifti.Serialization.Binary
{
    internal interface IIndexWriter<TKey> : IDisposable
    {
        Task WriteAsync(IIndexSnapshot<TKey> snapshot);
    }
}