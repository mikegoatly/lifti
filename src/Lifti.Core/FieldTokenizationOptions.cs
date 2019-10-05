using System;

namespace Lifti
{
    public class FieldTokenizationOptions<TItem>
    {
        public FieldTokenizationOptions(string name, Func<TItem, string> reader, TokenizationOptions? tokenizationOptions = null)
        {
            this.Name = name;
            this.Reader = reader;
            this.TokenizationOptions = tokenizationOptions ?? TokenizationOptions.Default;
        }

        public string Name { get; set; }

        public Func<TItem, string> Reader { get; set; }

        public TokenizationOptions TokenizationOptions { get; set; }
    }
}