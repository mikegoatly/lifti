using System.IO;

namespace Lifti.Serialization.Binary
{
    public interface IKeySerializer<TKey>
    {
        void Write(BinaryWriter writer, TKey key);
        TKey Read(BinaryReader reader);
    }
}
