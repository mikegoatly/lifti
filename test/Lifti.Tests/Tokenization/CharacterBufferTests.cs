using FluentAssertions;
using Lifti.Tokenization;
using System;
using Xunit;

namespace Lifti.Tests.Tokenization
{
    public class CharacterBufferTests
    {
        [Fact]
        public void Constructor_WithInitialCapacity_ShouldCreateBufferWithCorrectCapacity()
        {
            var buffer = new CharacterBuffer(100);

            buffer.Length.Should().Be(0);
            buffer.Capacity.Should().BeGreaterOrEqualTo(100);
            
            buffer.Dispose();
        }

        [Fact]
        public void Constructor_WithReadOnlySpan_ShouldCopyContent()
        {
            var source = "Hello World".AsSpan();
            var buffer = new CharacterBuffer(source);

            buffer.Length.Should().Be(11);
            buffer.ToString().Should().Be("Hello World");
            
            buffer.Dispose();
        }

        [Fact]
        public void Append_Char_ShouldAddCharacter()
        {
            var buffer = new CharacterBuffer(10);
            
            buffer.Append('H');
            buffer.Append('i');

            buffer.Length.Should().Be(2);
            buffer.ToString().Should().Be("Hi");
            
            buffer.Dispose();
        }

        [Fact]
        public void Append_String_ShouldAddAllCharacters()
        {
            var buffer = new CharacterBuffer(10);
            
            buffer.Append("Hello");
            buffer.Append(" World");

            buffer.Length.Should().Be(11);
            buffer.ToString().Should().Be("Hello World");
            
            buffer.Dispose();
        }

        [Fact]
        public void Append_ReadOnlySpan_ShouldAddAllCharacters()
        {
            var buffer = new CharacterBuffer(10);
            
            buffer.Append("Hello".AsSpan());
            buffer.Append(" ".AsSpan());
            buffer.Append("World".AsSpan());

            buffer.Length.Should().Be(11);
            buffer.ToString().Should().Be("Hello World");
            
            buffer.Dispose();
        }

        [Fact]
        public void Append_BeyondCapacity_ShouldGrowBuffer()
        {
            var buffer = new CharacterBuffer(2);
            
            buffer.Append("Hello");

            buffer.Length.Should().Be(5);
            buffer.ToString().Should().Be("Hello");
            
            buffer.Dispose();
        }

        [Fact]
        public void Indexer_Get_ShouldReturnCorrectCharacter()
        {
            var buffer = new CharacterBuffer("Test".AsSpan());

            buffer[0].Should().Be('T');
            buffer[1].Should().Be('e');
            buffer[2].Should().Be('s');
            buffer[3].Should().Be('t');
            
            buffer.Dispose();
        }

        [Fact]
        public void Indexer_Set_ShouldModifyCharacter()
        {
            var buffer = new CharacterBuffer("Test".AsSpan());

            buffer[0] = 'B';
            buffer[3] = 's';

            buffer.ToString().Should().Be("Bess");
            
            buffer.Dispose();
        }

        [Fact]
        public void Length_Set_ShouldTruncateContent()
        {
            var buffer = new CharacterBuffer("Hello World".AsSpan());

            buffer.Length = 5;

            buffer.ToString().Should().Be("Hello");
            
            buffer.Dispose();
        }

        [Fact]
        public void Length_SetToZero_ShouldClearContent()
        {
            var buffer = new CharacterBuffer("Hello".AsSpan());

            buffer.Length = 0;

            buffer.Length.Should().Be(0);
            buffer.ToString().Should().Be("");
            
            buffer.Dispose();
        }

        [Fact]
        public void Clear_ShouldResetLength()
        {
            var buffer = new CharacterBuffer("Hello".AsSpan());

            buffer.Clear();

            buffer.Length.Should().Be(0);
            buffer.ToString().Should().Be("");
            
            buffer.Dispose();
        }

        [Fact]
        public void Clear_ShouldAllowReuse()
        {
            var buffer = new CharacterBuffer("Hello".AsSpan());

            buffer.Clear();
            buffer.Append("World");

            buffer.ToString().Should().Be("World");
            
            buffer.Dispose();
        }

        [Fact]
        public void AsSpan_ShouldReturnCorrectContent()
        {
            var buffer = new CharacterBuffer("Hello".AsSpan());

            var span = buffer.AsSpan();

            span.Length.Should().Be(5);
            span.ToString().Should().Be("Hello");
            
            buffer.Dispose();
        }

        [Fact]
        public void AsMemory_ShouldReturnCorrectContent()
        {
            var buffer = new CharacterBuffer("Hello".AsSpan());

            var memory = buffer.AsMemory();

            memory.Length.Should().Be(5);
            memory.ToString().Should().Be("Hello");
            
            buffer.Dispose();
        }

        [Fact]
        public void AsSpan_AfterModification_ShouldReflectChanges()
        {
            var buffer = new CharacterBuffer("Hello".AsSpan());
            buffer[0] = 'J';
            buffer.Append(" World");

            var span = buffer.AsSpan();

            span.ToString().Should().Be("Jello World");
            
            buffer.Dispose();
        }

        [Fact]
        public void ToString_ShouldReturnCorrectString()
        {
            var buffer = new CharacterBuffer(10);
            buffer.Append("Test");
            buffer.Append(" ");
            buffer.Append("String");

            buffer.ToString().Should().Be("Test String");
            
            buffer.Dispose();
        }

        [Fact]
        public void Dispose_ShouldAllowSafeCleanup()
        {
            var buffer = new CharacterBuffer("Hello".AsSpan());
            
            buffer.Dispose();

            // Should not throw - dispose should be safe to call
            buffer.Dispose();
        }

        [Fact]
        public void MultipleOperations_ShouldWorkCorrectly()
        {
            var buffer = new CharacterBuffer(10);
            
            buffer.Append("Hello");
            buffer.Length.Should().Be(5);
            
            buffer.Append(' ');
            buffer.Length.Should().Be(6);
            
            buffer.Append("World");
            buffer.Length.Should().Be(11);
            
            buffer[6] = 'w';
            buffer.ToString().Should().Be("Hello world");
            
            buffer.Length = 5;
            buffer.ToString().Should().Be("Hello");
            
            buffer.Clear();
            buffer.Length.Should().Be(0);
            
            buffer.Append("New");
            buffer.ToString().Should().Be("New");
            
            buffer.Dispose();
        }

        [Fact]
        public void LargeContent_ShouldGrowCorrectly()
        {
            var buffer = new CharacterBuffer(2);
            
            // Add content that will require multiple growths
            for (int i = 0; i < 100; i++)
            {
                buffer.Append('X');
            }

            buffer.Length.Should().Be(100);
            buffer.ToString().Should().Be(new string('X', 100));
            
            buffer.Dispose();
        }

        [Fact]
        public void AsSpan_WithEmptyBuffer_ShouldReturnEmptySpan()
        {
            var buffer = new CharacterBuffer(10);

            var span = buffer.AsSpan();

            span.Length.Should().Be(0);
            span.IsEmpty.Should().BeTrue();
            
            buffer.Dispose();
        }

        [Fact]
        public void UnicodeCharacters_ShouldBeHandledCorrectly()
        {
            var buffer = new CharacterBuffer(10);
            
            buffer.Append("Hello");
            buffer.Append(" 世界");
            buffer.Append(" 🌍");

            var result = buffer.ToString();
            result.Should().Contain("世界");
            result.Should().Contain("🌍");
            
            buffer.Dispose();
        }
    }
}
