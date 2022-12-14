using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tokenization.Objects
{
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

        internal IEnumerable<IFieldReader> GetAllConfiguredFields()
        {
            return this.options.Values.SelectMany(o => o.GetConfiguredFields());
        }
    }
}
