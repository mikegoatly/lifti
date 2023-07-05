namespace Lifti
{
    /// <summary>
    /// The various kinds of field kinds that can be registered.
    /// </summary>
    public enum FieldKind
    {
        /// <summary>
        /// The field kind is unknown. Would indicate a bug if encountered at runtime.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The field was statically registered at index creation time.
        /// </summary>
        Static = 1,

        /// <summary>
        /// The field was dynamically registered during indexing.
        /// </summary>
        Dynamic = 2
    }
}
