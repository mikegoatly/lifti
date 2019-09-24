namespace Lifti
{
    public interface IIndexedFieldLookup
    {
        byte DefaultField { get; }

        string GetFieldForId(byte id);
        byte GetOrCreateIdForField(string fieldName);
    }
}