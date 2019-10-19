using System;
using System.Collections.Generic;

namespace Lifti
{
    internal class ConfiguredItemTokenizationOptions<TKey>
    {
        private readonly Dictionary<Type, object> options = new Dictionary<Type, object>();

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
    }
}
