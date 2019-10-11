using Lifti;
using Lifti.Tokenization.Stemming;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace TestConsole
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var stemmer = new PorterStemmer();

            var builder = new StringBuilder();
            using (var stream = typeof(Program).Assembly.GetManifestResourceStream(typeof(Program), "StemmerTestCases.txt"))
            {
                using (var reader = new StreamReader(stream))
                {
                    string line;
                    string[] testCase;
                    var space = new[] { ' ' };
                    while ((line = reader.ReadLine()) != null)
                    {
                        testCase = line.Split(space, StringSplitOptions.RemoveEmptyEntries);
                        if (testCase.Length != 2)
                        {
                            throw new Exception("Expected an array of two - word, stemmed word");
                        }

                        builder.Length = 0;
                        builder.Append(testCase[0]);
                        stemmer.Stem(builder);
                    }
                }
            }
        }
    }
}
