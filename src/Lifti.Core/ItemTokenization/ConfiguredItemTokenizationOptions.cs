using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.ItemTokenization
{
    internal class ConfiguredItemTokenizationOptions<TKey>
    {
        private readonly Dictionary<Type, IItemTokenizationOptions> options = new Dictionary<Type, IItemTokenizationOptions>();

        public void Add<TItem>(ItemTokenizationOptions<TItem, TKey> options)
        {
            this.options[typeof(TItem)] = options;
        }

        public ItemTokenizationOptions<TItem, TKey> Get<TItem>()
        {
            if (this.options.TryGetValue(typeof(TItem), out var itemTokenizationOptions))
            {
                return (ItemTokenizationOptions<TItem, TKey>)itemTokenizationOptions;
            }

            throw new LiftiException(ExceptionMessages.NoTokenizationOptionsProvidedForType, typeof(TItem));
        }

        internal IEnumerable<IFieldTokenization> GetAllConfiguredFields()
        {
            return this.options.Values.SelectMany(o => o.GetConfiguredFields());
        }
    }
}
