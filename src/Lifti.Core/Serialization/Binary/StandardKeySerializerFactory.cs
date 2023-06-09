using System;
using System.Collections.Generic;

namespace Lifti.Serialization.Binary
{
    internal static class StandardKeySerializerFactory
    {
        private static readonly Dictionary<Type, object> standardKeySerializers = new()
        {
            { typeof(string), new StringFormatterKeySerializer() },
            { typeof(int), new IntFormatterKeySerializer() },
            { typeof(uint), new UIntFormatterKeySerializer() },
            { typeof(Guid), new GuidFormatterKeySerializer() },
        };

        public static IKeySerializer<TKey> Create<TKey>()
        {
            if (standardKeySerializers.TryGetValue(typeof(TKey), out var serializer))
            {
                return (serializer as IKeySerializer<TKey>)!;
            }

            throw new LiftiException($"No standard key serializer exists for type {typeof(TKey).Name} - please provide a custom implementation of IKeySerializer<> when serializing/deserializing");
        }
    }
}
