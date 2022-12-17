using FluentAssertions;
using Lifti.Tokenization;
using System.Linq;
using Xunit;

namespace Lifti.Tests
{
    public class ThesaurusBuilderTests
    {
        private ThesaurusBuilder sut;

        public ThesaurusBuilderTests()
        {
            this.sut = new ThesaurusBuilder();
        }

        [Fact]
        public void AddingSameSynonyms_ValuesAreDeduplicated()
        {
            sut.AddSynonyms("one", "word", "one");

            VerifyResults(
                ("one", new[] { "one", "word" }),
                ("word", new[] { "one", "word" }));
        }

        [Fact]
        public void AddingSameSynonymsMultipleTimes_ValuesAreDeduplicated()
        {
            sut.AddSynonyms("one", "word", "one");
            sut.AddSynonyms("one", "word", "one", "same");

            VerifyResults(
                ("one", new[] { "one", "word", "same" }),
                ("word", new[] { "one", "word", "same" }),
                ("same", new[] { "one", "word", "same" }));
        }

        [Fact]
        public void AddingSameHyponyms_ValuesAreDeduplicated()
        {
            sut.AddHyponyms("mammal", "dog", "cat", "dog");

            VerifyResults(
                ("dog", new[] { "dog", "mammal" }),
                ("cat", new[] { "cat", "mammal" }));
        }

        [Fact]
        public void AddingSameHyponymsMultipleTimes_ValuesAreDeduplicated()
        {
            sut.AddHyponyms("mammal", "dog", "cat");
            sut.AddHyponyms("mammal", "dog", "cat", "mouse");

            VerifyResults(
                ("dog", new[] { "dog", "mammal" }),
                ("cat", new[] { "cat", "mammal" }),
                ("mouse", new[] { "mouse", "mammal" }));
        }

        [Fact]
        public void AddingSameHypernyms_ValuesAreDeduplicated()
        {
            sut.AddHypernyms("dog", "mammal", "vertebrate", "mammal");

            VerifyResults(
                ("dog", new[] { "dog", "mammal", "vertebrate" }));
        }

        [Fact]
        public void AddingSameHypernymsMultipleTimes_ValuesAreDeduplicated()
        {
            sut.AddHypernyms("dog", "mammal", "vertebrate");
            sut.AddHypernyms("dog", "mammal", "vertebrate", "canine");

            VerifyResults(
                ("dog", new[] { "dog", "mammal", "vertebrate", "canine" }));
        }

        [Fact]
        public void AddingHypernymsAndSynonymsForSameWord_OnlySetHypernymsOnCorrectWord()
        {
            sut.AddSynonyms("dog", "doggy");
            sut.AddHypernyms("dog", "mammal", "vertebrate", "canine");

            VerifyResults(
                ("dog", new[] { "dog", "doggy", "mammal", "vertebrate", "canine" }),
                ("doggy", new[] { "dog", "doggy" }));
        }

        [Fact]
        public void UsingStemmingTokenizer_ShouldCombineTermsThatResultInTheSameStemmedForm()
        {
            sut.AddSynonyms("HYPOCRITE", "DECEPTION");
            sut.AddSynonyms("HYPOCRITICAL", "DECEPTIVE", "INSINCERE");

            VerifyResults(
                new IndexTokenizer(new TokenizationOptions { Stemming = true }),
                ("HYPOCRIT", new[] { "HYPOCRIT", "DECEPT", "INSINCER" }),
                ("DECEPT", new[] { "HYPOCRIT", "DECEPT", "INSINCER" }),
                ("INSINCER", new[] { "HYPOCRIT", "DECEPT", "INSINCER" }));
        }

        private void VerifyResults(IIndexTokenizer tokenizer, params (string, string[])[] expected)
        {
            var actual = this.sut.Build(tokenizer);

            actual.WordLookup.Should().BeEquivalentTo(
                expected.ToDictionary(x => x.Item1, x => x.Item2));
        }

        private void VerifyResults(params (string, string[])[] expected)
        {
            this.VerifyResults(new IndexTokenizer(new TokenizationOptions { CaseInsensitive = false }), expected);
        }
    }
}
