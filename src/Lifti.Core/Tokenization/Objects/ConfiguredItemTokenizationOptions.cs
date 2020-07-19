using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tokenization.Objects
{
    internal class ConfiguredItemTokenizationOptions<TKey>
    {
        private readonly Dictionary<Type, IItemTokenization> options = new Dictionary<Type, IItemTokenization>();

        public void Add<TItem>(ItemTokenization<TItem, TKey> options)
        {
            this.options[typeof(TItem)] = options;
        }

        public ItemTokenization<TItem, TKey> Get<TItem>()
        {
            if (this.options.TryGetValue(typeof(TItem), out var itemTokenizationOptions))
            {
                return (ItemTokenization<TItem, TKey>)itemTokenizationOptions;
            }

            throw new LiftiException(ExceptionMessages.NoTokenizationOptionsProvidedForType, typeof(TItem));
        }

        internal IEnumerable<IFieldTokenization> GetAllConfiguredFields()
        {
            return this.options.Values.SelectMany(o => o.GetConfiguredFields());
        }
    }
}
