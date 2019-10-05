using System;
using System.Collections.Generic;

namespace Lifti
{
    public class ItemTokenizationOptions<TItem, TKey>
    {
        public ItemTokenizationOptions(
            Func<TItem, TKey> idReader,
            params FieldTokenizationOptions<TItem>[] fieldTokenization)
        {
            this.KeyReader = idReader;
            this.FieldTokenization = fieldTokenization;
        }

        public Func<TItem, TKey> KeyReader { get; }
        public IReadOnlyList<FieldTokenizationOptions<TItem>> FieldTokenization { get; }
    }
}
