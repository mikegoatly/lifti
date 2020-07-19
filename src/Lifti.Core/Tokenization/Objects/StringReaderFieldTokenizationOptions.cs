using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lifti.Tokenization.Objects
{
    /// <summary>
    /// A field tokenization capable of asynchronously reading s string for a field.
    /// </summary>
    /// <typeparam name="TItem">
    /// The type of item the field belongs to.
    /// </typeparam>
    public class StringReaderFieldTokenizationOptions<TItem> : FieldTokenization<TItem>
    {
        private readonly Func<TItem, string> reader;

        internal StringReaderFieldTokenizationOptions(string name, Func<TItem, string> reader, TokenizationOptions? tokenizationOptions = null)
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