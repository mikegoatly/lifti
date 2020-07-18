using Lifti.Tokenization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lifti.ItemTokenization
{
    /// <summary>
    /// A field tokenization capable of reading an enumerable of strings for a field.
    /// </summary>
    /// <typeparam name="TItem">
    /// The type of item the field belongs to.
    /// </typeparam>
    public class StringArrayReaderFieldTokenizationOptions<TItem> : FieldTokenizationOptions<TItem>
    {
        private readonly Func<TItem, IEnumerable<string>> reader;

        internal StringArrayReaderFieldTokenizationOptions(string name, Func<TItem, IEnumerable<string>> reader, TokenizationOptions? tokenizationOptions = null)
            : base(name, tokenizationOptions)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        internal override ValueTask<IReadOnlyList<Token>> TokenizeAsync(ITokenizer tokenizer, TItem item)
        {
            var tokens = tokenizer.Process(this.reader(item));
            return new ValueTask<IReadOnlyList<Token>>(tokens);
        }
    }
}