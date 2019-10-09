using FluentAssertions;
using Lifti.Tokenization.Stemming;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Lifti.Tests.Tokenization.Stemming
{
    public class PorterStemmerTests
    {
        /// <summary>
        /// Tests all the base test cases as specified in the files:
        /// http://snowball.tartarus.org/algorithms/porter/voc.txt and http://snowball.tartarus.org/algorithms/porter/output.txt
        /// </summary>
        [Fact]
        public void StemWordTest()
        {
            var stemmer = new PorterStemmer();

            using (var stream = typeof(PorterStemmerTests).Assembly.GetManifestResourceStream(typeof(PorterStemmerTests), "StemmerTestCases.txt"))
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

                        new string(stemmer.Stem(testCase[0].AsSpan()))
                            .Should().Be(testCase[1],because: "Stemming {0}", testCase[0]);
                    }
                }
            }
        }
    }
}