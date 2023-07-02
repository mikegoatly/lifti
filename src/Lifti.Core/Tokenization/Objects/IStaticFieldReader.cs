using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Tokenization.Objects
{
    /// <summary>
    /// Implemented by field readers that can be defined statically during index creation.
    /// </summary>
    internal interface IStaticFieldReader : IFieldConfig
    {
        /// <summary>
        /// Gets the name of the field. This can be referred to when querying to restrict searches to text read for this field only.
        /// </summary>
        string Name { get; }
    }

    /// <inheritdoc />
    internal interface IStaticFieldReader<TItem> : IStaticFieldReader
    {
        /// <summary>
        /// Reads the field's text from the given item.
        /// </summary>
        ValueTask<IEnumerable<string>> ReadAsync(TItem item, CancellationToken cancellationToken);
    }
}