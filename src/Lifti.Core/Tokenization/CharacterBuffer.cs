using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Lifti.Tokenization
{
    /// <summary>
    /// A mutable character buffer optimized for tokenization and stemming operations.
    /// Provides efficient character manipulation while minimizing allocations by working with Memory&lt;char&gt;.
    /// </summary>
    public ref struct CharacterBuffer
    {
        private char[] buffer;
        private int length;

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterBuffer"/> struct with the specified initial capacity.
        /// </summary>
        public CharacterBuffer(int initialCapacity)
        {
            this.buffer = ArrayPool<char>.Shared.Rent(initialCapacity);
            this.length = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterBuffer"/> struct from a ReadOnlySpan.
        /// </summary>
        public CharacterBuffer(ReadOnlySpan<char> source)
        {
            this.buffer = ArrayPool<char>.Shared.Rent(Math.Max(source.Length, 16));
            this.length = source.Length;
            source.CopyTo(this.buffer);
        }

        /// <summary>
        /// Gets or sets the length of the buffer. This will not resize the buffer, only adjust the logical length.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when setting a length less than zero or greater than the buffer capacity.</exception>"
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.length;
            set
            {
                if (value < 0 || value > this.buffer.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                this.length = value;
            }
        }

        /// <summary>
        /// Gets the capacity of the buffer.
        /// </summary>
        public readonly int Capacity => this.buffer.Length;

        /// <summary>
        /// Gets or sets the character at the specified index.
        /// </summary>
        public char this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.buffer[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this.buffer[index] = value;
        }

        /// <summary>
        /// Appends a character to the buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(char c)
        {
            if (this.length >= this.buffer.Length)
            {
                this.Grow();
            }

            this.buffer[this.length++] = c;
        }

        /// <summary>
        /// Appends a string to the buffer.
        /// </summary>
        public void Append(string text)
        {
            this.Append(text.AsSpan());
        }

        /// <summary>
        /// Appends a span of characters to the buffer.
        /// </summary>
        public void Append(ReadOnlySpan<char> chars)
        {
            var requiredLength = this.length + chars.Length;
            if (requiredLength > this.buffer.Length)
            {
                this.Grow(requiredLength);
            }

            chars.CopyTo(this.buffer.AsSpan(this.length));
            this.length += chars.Length;
        }

        /// <summary>
        /// Clears the buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            this.length = 0;
        }

        /// <summary>
        /// Gets the current content as a ReadOnlySpan.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<char> AsSpan()
        {
            return this.buffer.AsSpan(0, this.length);
        }

        /// <summary>
        /// Gets the current content as a ReadOnlyMemory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlyMemory<char> AsMemory()
        {
            return this.buffer.AsMemory(0, this.length);
        }

        /// <summary>
        /// Determines whether the end of the current span matches the specified suffix.
        /// </summary>
        /// <param name="suffix">The span of characters to compare to the end of the current span. The comparison is case-sensitive.</param>
        /// <returns>true if the end of the current span matches the specified suffix; otherwise, false.</returns>
        public readonly bool EndsWith(ReadOnlySpan<char> suffix)
        {
            if (suffix.Length > this.length)
            {
                return false;
            }

            return this.buffer.AsSpan().Slice(this.length - suffix.Length, suffix.Length)
                .SequenceEqual(suffix);
        }

        /// <summary>
        /// Determines whether the current span begins with the specified prefix.
        /// </summary>
        /// <param name="prefix">The span to compare against the beginning of the current span. Must not be longer than the current span.</param>
        /// <returns>true if the current span starts with the specified prefix; otherwise, false.</returns>
        public readonly bool StartsWith(ReadOnlySpan<char> prefix)
        {
            if (prefix.Length > this.length)
            {
                return false;
            }

            return this.buffer.AsSpan(0, prefix.Length)
                .SequenceEqual(prefix);
        }

        /// <summary>
        /// Determines whether the sequence of characters in the current instance is equal to the sequence in the
        /// specified read-only character span.
        /// </summary>
        /// <param name="other">A read-only span of characters to compare with the current instance.</param>
        /// <returns>true if the sequences are equal in length and contain the same characters in the same order; otherwise,
        /// false.</returns>
        public readonly bool SequenceEqual(ReadOnlySpan<char> other)
        {
            if (other.Length != this.length)
            {
                return false;
            }

            return this.buffer.AsSpan(0, this.length).SequenceEqual(other);
        }

        /// <summary>
        /// Returns the buffer to the pool and resets the state.
        /// </summary>
        public void Dispose()
        {
            if (this.buffer is not null)
            {
                ArrayPool<char>.Shared.Return(this.buffer);
                this.length = 0;

                // Prevent double return and guard against use after dispose
                this.buffer = null!;
            }
        }

        /// <summary>
        /// Converts the buffer to a string.
        /// </summary>
        public override readonly string ToString()
        {
            return new string(this.buffer, 0, this.length);
        }

        private void Grow(int? minimumCapacity = null)
        {
            var newCapacity = minimumCapacity ?? this.buffer.Length * 2;
            if (newCapacity < this.buffer.Length * 2)
            {
                newCapacity = this.buffer.Length * 2;
            }

            var newBuffer = ArrayPool<char>.Shared.Rent(newCapacity);
            Array.Copy(this.buffer, newBuffer, this.length);
            ArrayPool<char>.Shared.Return(this.buffer);
            this.buffer = newBuffer;
        }
    }
}
