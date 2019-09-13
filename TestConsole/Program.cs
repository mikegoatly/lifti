using Lifti;
using Lifti.Preprocessing;
using System;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var index = new FullTextIndex<string>(
                new FullTextIndexOptions<string>
                {
                    TokenizationOptions = { SplitOnPunctuation = true },
                    Advanced = { SupportIntraNodeTextAfterCharacterIndex = 4 }
                },
                new XmlTokenizer(new InputPreprocessorPipeline(new IInputPreprocessor[]
                {
                    new CaseInsensitiveNormalizer(),
                    new LatinCharacterNormalizer()
                })),
                new IndexNodeFactory());
        }
    }
}
