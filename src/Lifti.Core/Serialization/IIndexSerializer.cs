using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Serialization
{
    internal interface IIndexSerializer<TKey> : IDisposable
    {
        ValueTask WriteAsync(IIndexSnapshot<TKey> snapshot, CancellationToken cancellationToken = default);
    }
}