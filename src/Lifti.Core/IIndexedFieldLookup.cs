namespace Lifti
{
    public interface IIndexedFieldLookup
    {
        byte DefaultField { get; }

        string GetFieldForId(byte id);
        IndexedFieldDetails GetFieldInfo(string fieldName);
    }
}