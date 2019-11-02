using Lifti.Tokenization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lifti.ItemTokenization
{
    public class StringArrayReaderFieldTokenizationOptions<TItem> : FieldTokenizationOptions<TItem>
    {
        private readonly Func<TItem, IEnumerable<string>> reader;

        internal StringArrayReaderFieldTokenizationOptions(string name, Func<TItem, IEnumerable<string>> reader, TokenizationOptions tokenizationOptions = null)
            : base(name, tokenizationOptions)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        internal override ValueTask<IEnumerable<Token>> TokenizeAsync(ITokenizer tokenizer, TItem item)
        {
            return new ValueTask<IEnumerable<Token>>(this.Tokenize(tokenizer, item));
        }

        internal override IEnumerable<Token> Tokenize(ITokenizer tokenizer, TItem item)
        {
            return tokenizer.Process(this.reader(item));
        }
    }
}