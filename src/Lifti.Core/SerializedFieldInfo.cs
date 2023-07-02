namespace Lifti
{
    /// <summary>
    /// A record that describes a information for a field when it was serialized.
    /// </summary>
    internal record SerializedFieldInfo(byte FieldId, string Name, FieldKind Kind, string? DynamicFieldReaderName);
}
