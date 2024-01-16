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
        private readonly Dictionary<Type, IObjectTypeConfiguration> options;

        public ObjectTypeConfigurationLookup(IEnumerable<IObjectTypeConfiguration> objectTypeConfigurations)
        {
            this.options = objectTypeConfigurations.ToDictionary(x => x.ObjectType);
        }

        public IEnumerable<IObjectTypeConfiguration> AllConfigurations => this.options.Values;

        public ObjectTypeConfiguration<TObject, TKey> Get<TObject>()
        {
            if (this.options.TryGetValue(typeof(TObject), out var objectTypeConfiguration))
            {
                return (ObjectTypeConfiguration<TObject, TKey>)objectTypeConfiguration;
            }

            throw new LiftiException(ExceptionMessages.NoTokenizationOptionsProvidedForType, typeof(TObject));
        }
    }
}
