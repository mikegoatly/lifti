using Lifti.Serialization.Binary;
using System.IO;

namespace TestConsole
{
    public class CompositeKeySerializer : IKeySerializer<CompositeKey>
    {
        public void Write(BinaryWriter writer, CompositeKey key)
        {
            writer.Write(key.UserId); // Int32
            writer.Write(key.CompanyId); // Int16
        }

        public CompositeKey Read(BinaryReader reader)
        {
            // The serialization framework will make sure this method is only
            // ever called when a key is ready to be read.
            // Ensure the data is read is read out in exactly the same order and with the same 
            // data types it was written.
            var userId = reader.ReadInt32();
            var companyId = reader.ReadInt16();

            return new CompositeKey(userId, companyId);
        }
    }
}
