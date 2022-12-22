using System;
using System.Collections.Generic;

namespace Lifti.Tokenization.TextExtraction
{
    /// <summary>
    /// Information about a fragment of extracted text from a larger document body.
    /// </summary>
    public readonly struct DocumentTextFragment : IEquatable<DocumentTextFragment>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentTextFragment"/> struct.
        /// </summary>
        public DocumentTextFragment(int offset, ReadOnlyMemory<char> text)
        {
            this.Offset = offset;
            this.Text = text;
        }

        /// <summary>
        /// Gets the offset of this fragment from the start of the overall document. This is useful when a document contains
        /// markup (e.g. HTML) and only text is being indexed. This offset allows for the overall offset 
        /// of produced tokens to relate to the complete document text.
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// Gets the text for this fragment.
        /// </summary>
        public ReadOnlyMemory<char> Text { get; }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is DocumentTextFragment fragment &&
                   this.Equals(fragment);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(this.Offset, this.Text);
        }

        /// <inheritdoc />
        public bool Equals(DocumentTextFragment other)
        {
            return this.Offset == other.Offset &&
                   EqualityComparer<ReadOnlyMemory<char>>.Default.Equals(this.Text, other.Text);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(DocumentTextFragment left, DocumentTextFragment right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(DocumentTextFragment left, DocumentTextFragment right)
        {
            return !(left == right);
        }
    }
}
