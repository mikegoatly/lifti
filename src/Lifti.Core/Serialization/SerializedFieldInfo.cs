namespace Lifti.Serialization
{
    /// <summary>
    /// A record that describes a information for a field when it was serialized.
    /// </summary>
    public record struct SerializedFieldInfo(byte FieldId, string Name, FieldKind Kind, string? DynamicFieldReaderName);
}
