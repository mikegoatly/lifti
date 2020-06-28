using Lifti.Tokenization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lifti.ItemTokenization
{
    public class StringReaderFieldTokenizationOptions<TItem> : FieldTokenizationOptions<TItem>
    {
        private readonly Func<TItem, string> reader;

        internal StringReaderFieldTokenizationOptions(string name, Func<TItem, string> reader, TokenizationOptions? tokenizationOptions = null)
            : base(name, tokenizationOptions)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        internal override ValueTask<IReadOnlyList<Token>> TokenizeAsync(ITokenizer tokenizer, TItem item)
        {
            return new ValueTask<IReadOnlyList<Token>>(this.Tokenize(tokenizer, item));
        }

        internal override IReadOnlyList<Token> Tokenize(ITokenizer tokenizer, TItem item)
        {
            return tokenizer.Process(this.reader(item));
        }
    }
}