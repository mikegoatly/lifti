using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tokenization.Objects
{
    /// <summary>
    /// Defines a lookup for all the field readers associated to a given object type.
    /// </summary>
    /// <typeparam name="TKey">The type of key in the index.</typeparam>
    internal class ObjectTypeConfigurationLookup<TKey>
    {
        private readonly Dictionary<Type, IIndexedObjectConfiguration> options;

        public ObjectTypeConfigurationLookup(IEnumerable<IIndexedObjectConfiguration> objectTokenizers)
        {
            this.options = objectTokenizers.ToDictionary(x => x.ItemType);
        }

        public IEnumerable<IIndexedObjectConfiguration> AllConfigurations => this.options.Values;

        public IndexedObjectConfiguration<TItem, TKey> Get<TItem>()
        {
            if (this.options.TryGetValue(typeof(TItem), out var itemTokenizationOptions))
            {
                return (IndexedObjectConfiguration<TItem, TKey>)itemTokenizationOptions;
            }

            throw new LiftiException(ExceptionMessages.NoTokenizationOptionsProvidedForType, typeof(TItem));
        }
    }
}
