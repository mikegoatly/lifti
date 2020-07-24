using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tokenization.Objects
{
    internal class ConfiguredObjectTokenizationOptions<TKey>
    {
        private readonly Dictionary<Type, IObjectTokenization> options = new Dictionary<Type, IObjectTokenization>();

        public void Add<TItem>(ObjectTokenization<TItem, TKey> options)
        {
            this.options[typeof(TItem)] = options;
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
