using FluentAssertions;
using Lifti.Tokenization;
using Lifti.Tokenization.Objects;
using Lifti.Tokenization.Stemming;
using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Lifti.Tests
{
    public class IndexedFieldLookupTests
    {
        private static readonly DynamicFieldReader<TestObject> dynamicFieldReader = CreateDynamicFieldReader<TestObject>();

        private readonly IndexedFieldLookup sut;

        public IndexedFieldLookupTests()
        {
            this.sut = new IndexedFieldLookup();
        }

        [Fact]
        public void GettingIdsForFieldsShouldReturnCorrectIncrementalIds()
        {
            this.WithBasicConfig();

            this.sut.GetFieldInfo("Field1").Id.Should().Be(1);
            this.sut.GetFieldInfo("Field2").Id.Should().Be(2);
            this.sut.GetFieldInfo("Field3").Id.Should().Be(3);
        }

        [Fact]
        public async Task StaticFieldsShouldBeRegisteredCorrectly()
        {
            this.WithBasicConfig();

            var fieldInfo = this.sut.GetFieldInfo("Field1");
            fieldInfo.TextExtractor.Should().BeOfType<PlainTextExtractor>();
            fieldInfo.Thesaurus.Should().NotBeNull();
            fieldInfo.FieldKind.Should().Be(FieldKind.Static);
            fieldInfo.Tokenizer.Should().NotBeNull();
            var readField = await fieldInfo.ReadAsync("foo", default);
            readField.Should().BeEquivalentTo(["foo".AsMemory()]);
        }

        [Fact]
        public async Task DynamicFieldsShouldBeRegisteredCorrectly()
        {
            var fieldInfo = this.sut.GetOrCreateDynamicFieldInfo(
                dynamicFieldReader,
                "foo");

            fieldInfo.Name.Should().Be("foo");
            fieldInfo.TextExtractor.Should().BeOfType<PlainTextExtractor>();
            fieldInfo.Thesaurus.Should().NotBeNull();
            fieldInfo.FieldKind.Should().Be(FieldKind.Dynamic);
            fieldInfo.Tokenizer.Should().NotBeNull();
            var readField = await fieldInfo.ReadAsync(new TestObject(), default);
            readField.Should().BeEquivalentTo(["bar".AsMemory()]);
        }

        [Fact]
        public void ShouldThrowExceptionIfDynamicFieldRegisteredWithSameNameAsStaticField()
        {
            this.WithBasicConfig();

            Assert.Throws<LiftiException>(() => this.sut.GetOrCreateDynamicFieldInfo(
                dynamicFieldReader,
                "Field1")).Message.Should().Be("Cannot register a dynamic field with the same name as the statically registered field \"Field1\". Consider using a field prefix when configuring the dynamic fields.");
        }

        [Fact]
        public void ShouldGetTheSameFieldInfoInstanceWhenGettingOrCreatingDynamicFieldInfoMultipleTimesWithSameConfig()
        {
            var field1 = this.sut.GetOrCreateDynamicFieldInfo(dynamicFieldReader, "Field1");
            var field2 = this.sut.GetOrCreateDynamicFieldInfo(dynamicFieldReader, "Field2");

            this.sut.GetOrCreateDynamicFieldInfo(dynamicFieldReader, "Field1").Should().Be(field1);
            this.sut.GetOrCreateDynamicFieldInfo(dynamicFieldReader, "Field2").Should().Be(field2);
        }

        [Fact]
        public void ShouldThrowExceptionIfDynamicFieldRegisteredWithSameNameAgainstDifferentObjectType()
        {
            this.sut.GetOrCreateDynamicFieldInfo(dynamicFieldReader, "Field1");

            Assert.Throws<LiftiException>(() => this.sut.GetOrCreateDynamicFieldInfo(
                CreateDynamicFieldReader<TestObject2>(),
                "Field1")).Message.Should().Be("Cannot register dynamic field with the same name \"Field1\" against different object types. Consider using a field prefix when configuring the dynamic fields.");
        }

        [Fact]
        public void GettingTokenizationOptionsShouldReturnCorrectlyConstructedInstances()
        {
            this.WithBasicConfig();

            ((IndexTokenizer)this.sut.GetFieldInfo("FieldX").Tokenizer).Options.Stemmer.Should().BeOfType<PorterStemmer>();
            ((IndexTokenizer)this.sut.GetFieldInfo("FieldY").Tokenizer).Options.Stemmer.Should().BeNull();
        }

        [Fact]
        public void GettingNameForValidIdShouldReturnCorrectFieldName()
        {
            this.WithBasicConfig();

            this.sut.GetFieldForId(2).Should().Be("Field2");
            this.sut.GetFieldForId(1).Should().Be("Field1");
        }

        [Fact]
        public void GettingNameForInvalidIdShouldThrowException()
        {
            Assert.Throws<LiftiException>(() => this.sut.GetFieldForId(99))
                .Message.Should().Be("Field id 99 has no associated field name");
        }

        [Fact]
        public void UsingMoreThan255IdsShouldThrowException()
        {
            var itemConfigBuilder = new ObjectTokenizationBuilder<string, string>()
                .WithKey(i => i);

            for (var i = 0; i < 256; i++)
            {
                itemConfigBuilder = itemConfigBuilder.WithField("Field" + i, r => r);
            }

            Assert.Throws<LiftiException>(() => this.Build(itemConfigBuilder, this.sut))
                .Message.Should().Be("Only 255 distinct fields can currently be indexed");
        }

        [Fact]
        public void UsingDuplicateFieldNameShouldThrowException()
        {
            var config1 = this.Build(new ObjectTokenizationBuilder<string, string>()
                .WithField("Field1", o => o)
                .WithField("Field2", o => o)
                .WithKey(i => i), this.sut);

            Assert.Throws<LiftiException>(
                () => this.Build(new ObjectTokenizationBuilder<string, string>()
                    .WithField("Field1", o => o)
                    .WithKey(i => i), this.sut))
                .Message.Should().Be("Duplicate field name used: Field1. Field names must be unique across all item types registered with an index.");
        }

        private void WithBasicConfig()
        {
            this.Build(
                new ObjectTokenizationBuilder<string, string>()
                    .WithKey(i => i)
                    .WithField("Field1", r => r)
                    .WithField("Field2", r => r)
                    .WithField("Field3", r => r)
                    .WithField("FieldX", r => r, o => o.WithStemming())
                    .WithField("FieldY", r => r),
                this.sut);
        }

        private IObjectTypeConfiguration Build(ObjectTokenizationBuilder<string, string> objectTokenizationBuilder, IndexedFieldLookup fieldLookup)
        {
            return (objectTokenizationBuilder as IObjectTokenizationBuilder)
                .Build(
                    1,
                    IndexTokenizer.Default,
                    new ThesaurusBuilder(),
                    new PlainTextExtractor(),
                    fieldLookup);
        }

        private static DynamicFieldReader<TObject> CreateDynamicFieldReader<TObject>()
            where TObject : new()
        {
            var reader = new StringDictionaryDynamicFieldReader<TObject>(
                x => new Dictionary<string, string> { { "foo", "bar" } },
                "Test",
                null,
                IndexTokenizer.Default,
                new PlainTextExtractor(),
                new ThesaurusBuilder().Build(IndexTokenizer.Default),
                1D);

            // Force the reader to first produce (and cache) the field names
            reader.ReadAsync(new TObject(), default);

            return reader;
        }

        private class TestObject
        {
        }

        private class TestObject2
        {
        }
    }
}
