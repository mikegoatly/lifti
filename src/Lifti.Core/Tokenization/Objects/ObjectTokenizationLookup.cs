using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tokenization.Objects
{
    /// <summary>
    /// Defines a lookup for all the field readers associated to a given object type.
    /// </summary>
    /// <typeparam name="TKey">The type of key in the index.</typeparam>
    internal class ObjectTokenizationLookup<TKey>
    {
        private readonly Dictionary<Type, IObjectTokenization> options;

        public ObjectTokenizationLookup(IEnumerable<IObjectTokenization> objectTokenizers)
        {
            this.options = objectTokenizers.ToDictionary(x => x.ItemType);
        }

        public ObjectTokenization<TItem, TKey> Get<TItem>()
        {
            if (this.options.TryGetValue(typeof(TItem), out var itemTokenizationOptions))
            {
                return (ObjectTokenization<TItem, TKey>)itemTokenizationOptions;
            }

            throw new LiftiException(ExceptionMessages.NoTokenizationOptionsProvidedForType, typeof(TItem));
        }
    }
}
