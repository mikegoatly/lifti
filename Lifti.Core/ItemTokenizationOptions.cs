using System;

namespace Lifti
{
    public class ItemTokenizationOptions<TItem, TKey>
    {
        public ItemTokenizationOptions(
            Func<TItem, TKey> idReader,
            params FieldTokenization<TItem>[] fieldTokenization)
        {
            this.KeyReader = idReader;
            this.FieldTokenization = fieldTokenization;
        }

        public Func<TItem, TKey> KeyReader { get; }
        public FieldTokenization<TItem>[] FieldTokenization { get; }
    }
}
