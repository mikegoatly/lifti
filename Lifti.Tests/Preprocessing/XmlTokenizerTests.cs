﻿using FluentAssertions;
using Lifti.Preprocessing;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Preprocessing
{
    public class XmlTokenizerTests
    {
        private readonly XmlTokenizer sut;

        public XmlTokenizerTests()
        {
            this.sut = new XmlTokenizer();
            ((ITokenizer)this.sut).Configure(new TokenizationOptions());
        }

        [Fact]
        public void ShouldSkipAllTextInXmlTags()
        {
            var output = this.sut.Process("test <data tag='foo'>inside</data>outside").ToList();

            output.OrderBy(o => o.Value[0]).Should().BeEquivalentTo(new[]
            {
                new Token("inside", new Range(21, 6)),
                new Token("outside", new Range(34, 7)),
                new Token("test", new Range(0, 4))
            });
        }
    }
}
